using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using StigsUtils.Extensions;

namespace StigsUtils.SqlServer;

public class ScriptRunner : IDisposable
{
  public Action<string>? LogCallback { get; set; }
  private static readonly Regex _tableNameRegex = new(@"CREATE TABLE (?'tableName'[^\(]+)\(", RegexOptions.Compiled | RegexOptions.IgnoreCase);
  private readonly SqlConnection _connection;
  private readonly HashSet<Regex> _fileExcludeList = new();
  private readonly Server _server;
  private readonly ServerConnection _serverConnection;
  private List<string> _constraints = new();
  private List<string> _fkConstraints = new();
  public string? Dir { get; set; }

  public IEnumerable<Regex> FileExcludeList => _fileExcludeList;
  public ScriptRunner(string connectionString)
  {
    _connection = new SqlConnection(connectionString);
    _serverConnection = new ServerConnection(_connection);
    _server = new Server(_serverConnection);
  }
  public void Dispose()
  {
    while (_serverConnection.TransactionDepth > 0) _serverConnection.RollBackTransaction();
    _serverConnection.ForceDisconnected();
    _connection.Dispose();
  }
  public ScriptRunner BeginTransaction()
  {
    _serverConnection.BeginTransaction();
    return this;
  }
  public ScriptRunner CommitTransaction()
  {
    _serverConnection.CommitTransaction();
    return this;
  }
  public ScriptRunner RollbackTransaction()
  {
    _serverConnection.RollBackTransaction();
    return this;
  }
  public ScriptRunner CreateSchemas(string owner, params string[] schemaNames)
  {
    foreach (var name in schemaNames)
    {
      RunSql($"""
              IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'{name}')
              BEGIN
                  EXEC('CREATE SCHEMA {name}')
              END
              """);
    }
    return this;
  }

  public ScriptRunner RunSqlFile(string filePath)
  {
    if (!FileExcludeList.Any(x => x.IsMatch(filePath)))
    {
      try
      {
        RunSql(File.ReadAllText(filePath));
      }
      catch (Exception ex)
      {
        throw new ArgumentException($"Error running SQL file {filePath}:\n{ex}", ex);
      }
    }
    return this;
  }
  public ScriptRunner Exclude(string s)
  {
    _fileExcludeList.Add(new Regex(s, RegexOptions.Compiled));
    return this;
  }

  /// <summary>
  ///   Runs all sql files in a directory, with an optional execution order for some of the files.
  /// </summary>
  public ScriptRunner RunTableSqlFiles(string dir)
  {
    foreach (var filePath in Directory.EnumerateFiles(dir, "*.sql").NotMatchingAny(FileExcludeList))
    {
      (var sql, var constraints) = SimpleLinebasedSplitTableSqlScript(filePath);
      foreach (var constraint in constraints)
      {
        if(constraint.Contains(" FOREIGN KEY (")) _fkConstraints.Add(constraint);
        else _constraints.Add(constraint);
      }
      RunSql(sql);
    }
    return this;
  }
  /// <summary>
  ///   Runs all sql files in a directory, with an optional execution order for some of the files.
  /// </summary>
  public ScriptRunner RunSqlFiles(string dir, params string[] fileOrder)
  {
    var executedFiles = new HashSet<string>();
    foreach (var file in fileOrder)
    {
      var filepath = Path.GetFullPath(Path.Combine(dir, Path.GetFileNameWithoutExtension(file) + ".sql"));
      RunSqlFile(filepath);
      executedFiles.Add(filepath);
    }
    foreach (var file in Directory.EnumerateFiles(dir, "*.sql").Except(executedFiles))
    {
      RunSqlFile(file);
    }
    return this;
  }
  public ScriptRunner RunSql(string sql)
  {
    LogCallback?.Invoke(sql);
    _server.ConnectionContext.ExecuteNonQuery(sql);
    return this;
  }

  public (string CreateTableScript, List<string> ConstraintsScript) SimpleLinebasedSplitTableSqlScript(string filePath, bool includeIfExists = false)
  {
    StringBuilder createTableScriptBuilder = new();
    List<string> constraints = new();
    string? tableName = null;
    string? createTableScript = null;
    foreach (var l in File.ReadLines(filePath))
    {
      if (tableName == null)
      {
        var match = _tableNameRegex.Match(l);
        if (match.Success) tableName = match.Groups["tableName"].Value;
      }
      var line = l.Trim();
      if (line.StartsWith("CONSTRAINT ", StringComparison.InvariantCultureIgnoreCase) && !line.Contains("PRIMARY KEY"))
      {
        if (createTableScript == null)
        {
          createTableScript = createTableScriptBuilder.ToString().Trim().TrimEnd(',');
          createTableScriptBuilder.Clear();
        }
        var constraintStatement = CreateConstraintStatement(tableName!, line.TrimEnd(','));
        constraints.Add(constraintStatement);
      }
      else createTableScriptBuilder.AppendLine(line);
    }
    return (createTableScript+createTableScriptBuilder, constraints);
  }

  private string CreateConstraintStatement(string tableName, string constraint) => $"""
    ALTER TABLE {tableName}
    ADD {constraint};
  """;

  public void ApplyConstraints()
  {
    foreach (var x in _constraints.Concat(_fkConstraints)) // run foreign key constraints after other constraints.
    {
        RunSql(x);
    }
  }
}