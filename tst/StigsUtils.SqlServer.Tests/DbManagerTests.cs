// Copyright © 2023 TradingLens. All Rights Reserved.

using FluentAssertions;
using Xunit;

namespace StigsUtils.SqlServer.Tests;

public class DbManagerTests
{
  [Fact]
  public void CanCreateNewDatabaseFromScriptFiles()
  {
    CanCreateNewDatabase(
      SqlServerDbManager
       .UseLocalHostDb("Server=localhost,1433;Database=StigUtilsTests;User Id=sa;Password=padb3,2023;Connect Timeout=5;Encrypt=false;TrustServerCertificate=true")
       .DropDatabaseIfExists()
       .SetWorkingDir("TestData/Sql")
       .RunScriptFiles("CreateTables.StigUtilsTests.sql"));
  }

  [Fact]
  public void CanCreateNewDatabaseFromScripts()
  {
    CanCreateNewDatabase(
      SqlServerDbManager
       .UseLocalHostDb("Server=localhost,1433;Database=StigUtils_CanCreateNewDatabaseFromScripts;User Id=sa;Password=padb3,2023;Connect Timeout=5;Encrypt=false;TrustServerCertificate=true")
       .DropDatabaseIfExists()
       .RunScripts("""
                   CREATE TABLE [dbo].[AuditLog] (
                       [Id] UNIQUEIDENTIFIER NOT NULL,
                       PRIMARY KEY CLUSTERED ([Id] ASC),
                   );
                   GO
                   """));
  }

  private void CanCreateNewDatabase(SqlServerDbManager dbManager)
  {
    var id = Guid.NewGuid();
    dbManager.Execute("insert into [dbo].[AuditLog] (Id) values (@id)", new { id }).Should().Be(1);
    dbManager.ReadSingle<Guid>("select top 1 Id from [dbo].[AuditLog]").Should().Be(id);
  }
}