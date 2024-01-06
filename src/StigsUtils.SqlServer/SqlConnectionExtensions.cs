// Copyright © 2023 TradingLens. All Rights Reserved.

using System.Data;
using Dapper;

namespace StigsUtils.SqlServer;

public static class SqlConnectionExtensions 
{
  public static int ExecuteIfNotExists(this IDbConnection connection, string objectId, string sql)
  {
    var notExists = connection.ExecuteScalar<object?>($"SELECT OBJECT_ID(N'{objectId}')") == null; 
    return notExists ? connection.Execute(sql) : 0;
  }
}