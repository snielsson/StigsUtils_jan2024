﻿// Copyright © 2023 TradingLens. All Rights Reserved.

using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Serilog.Events;
using StigsUtils.Extensions;

namespace StigsUtils.SqlServer;

public class SqlServerActivityLog : ILogWriter
{
  public SqlServerActivityLog(IConfiguration configuration)
  {
    _connectionString = configuration.GetString("AuditLog:SqlServerConnectionString");
  }
  
  private const string ActivityLogTableName = "[dbo].[ActivityLog]";
  private const string CreateActivityLogTable =
    $"""
     CREATE TABLE {ActivityLogTableName} (
         [Id] UNIQUEIDENTIFIER NOT NULL,
         [LogLevel] INT NOT NULL,
         [LogEventId] INT NOT NULL,
         [AuditLevel] INT NOT NULL,
         [ActivityTraceId] UNIQUEIDENTIFIER NOT NULL,
         [ActivitySpanId] BIGINT NOT NULL,
         [UserId] UNIQUEIDENTIFIER NOT NULL,
         [ProjectId] UNIQUEIDENTIFIER,
         [SubProjectId] UNIQUEIDENTIFIER,
         [Message] NVARCHAR(MAX),
         [Exception] NVARCHAR(MAX),
         [HasException] AS (CASE WHEN [Exception] IS NOT NULL THEN 1 ELSE 0 END) PERSISTED,
         PRIMARY KEY CLUSTERED ([Id] ASC) -- Assuming Id is an ordered ULID and not a GUID
     );
     CREATE NONCLUSTERED INDEX IDX_ActivityLog_LogLevel_IsAudit ON {ActivityLogTableName} (LogLevel, AuditLevel);
     CREATE NONCLUSTERED INDEX IDX_ActivityLog_LogEventId ON {ActivityLogTableName} (LogEventId);
     CREATE NONCLUSTERED INDEX IDX_ActivityLog_AuditLevel ON {ActivityLogTableName} (AuditLevel);
     CREATE NONCLUSTERED INDEX IDX_ActivityLog_ActivityTraceId_ActivitySpanId ON {ActivityLogTableName} (ActivityTraceId, ActivitySpanId);
     CREATE NONCLUSTERED INDEX IDX_ActivityLog_UserId ON {ActivityLogTableName} (UserId);
     CREATE NONCLUSTERED INDEX IDX_ActivityLog_ProjectId ON {ActivityLogTableName} (ProjectId);
     CREATE NONCLUSTERED INDEX IDX_ActivityLog_SubProjectId ON {ActivityLogTableName} (SubProjectId);
     CREATE NONCLUSTERED INDEX IDX_ActivityLog_HasException ON {ActivityLogTableName} (HasException);
     """;

  private const string CreateInsertActivityLog =
    $"""
     CREATE PROCEDURE [dbo].[InsertActivityLog]
         @Id UNIQUEIDENTIFIER,
         @LogLevel INT,
         @LogEventId INT,
         @AuditLevel INT,
         @ActivityTraceId UNIQUEIDENTIFIER,
         @ActivitySpanId BIGINT,
         @UserId UNIQUEIDENTIFIER,
         @ProjectId UNIQUEIDENTIFIER = NULL,
         @SubProjectId UNIQUEIDENTIFIER = NULL,
         @Message NVARCHAR(MAX) = NULL,
         @Exception NVARCHAR(MAX) = NULL
     AS
     BEGIN
         INSERT INTO {ActivityLogTableName}
         (
            [Id],
            [LogLevel],
            [LogEventId],
            [AuditLevel],
            [ActivityTraceId],
            [ActivitySpanId],
            [UserId],
            [ProjectId],
            [SubProjectId],
            [Message],
            [Exception]
          ) VALUES
          (
            @Id,
            @LogLevel,
            @LogEventId,
            @AuditLevel,
            @ActivityTraceId,
            @ActivitySpanId,
            @UserId,
            @ProjectId,
            @SubProjectId,
            @Message,
            @Exception
          );
     END;
     """;
  private readonly string _connectionString;

  public async Task WriteLog(LogEvent logEvent) 
  { 
    await using var cmd = new SqlCommand("[dbo].[InsertActivityLog]");
    cmd.CommandType = CommandType.StoredProcedure;
    cmd.Parameters.AddWithValue("@Id", Ulid.NewUlid(logEvent.Timestamp).ToGuid());
    cmd.Parameters.AddWithValue("@LogLevel", (int)logEvent.Level);
      
    int? logEventId = 0;
    if (logEvent.Properties.TryGetValue("EventId", out LogEventPropertyValue? eventIdValue))
    {
      if (eventIdValue is StructureValue s)
      {
        if (s.Properties.FirstOrDefault()?.Value is ScalarValue x) logEventId = x.Value as int?;
      }
    }
    cmd.Parameters.AddWithValue("@LogEventId", logEventId ?? 0);

    int auditLevel = 0;
    if (logEvent.Properties.TryGetValue("AuditLevel", out LogEventPropertyValue? auditLevelPropertyValue))
    {
      auditLevel = int.Parse(auditLevelPropertyValue.ToString());
    }
    cmd.Parameters.AddWithValue("@AuditLevel", auditLevel);
      
    cmd.Parameters.AddWithValue("@ActivityTraceId", Activity.Current?.TraceId.ToGuid());
    cmd.Parameters.AddWithValue("@ActivitySpanId", Activity.Current?.SpanId.ToLong());
    cmd.Parameters.AddWithValue("@UserId", Activity.Current?.GetBaggageItem("UserId").AsGuid() ?? Guid.Empty);

    if (logEvent.Properties.TryGetValue("ProjectId", out LogEventPropertyValue? projectIdValue)) 
    {
      cmd.Parameters.AddWithValue("@ProjectId", Guid.Parse(projectIdValue.ToString()));
    }

    if (logEvent.Properties.TryGetValue("SubProjectId", out LogEventPropertyValue? subProjectIdValue)) 
    {
      cmd.Parameters.AddWithValue("@SubProjectId", Guid.Parse(subProjectIdValue.ToString()));
    }
      
    cmd.Parameters.AddWithValue("@Message", logEvent.RenderMessage());
      
    if (logEvent.Exception != null) 
    { 
      cmd.Parameters.AddWithValue("@Exception", logEvent.Exception);      
    }
    await using var db = new SqlConnection(_connectionString);
    await db.OpenAsync();
    cmd.Connection = db;
    await cmd.ExecuteNonQueryAsync();
  }

}
public interface ILogWriter { }