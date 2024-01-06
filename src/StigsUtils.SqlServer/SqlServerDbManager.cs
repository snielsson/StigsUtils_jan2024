using System.Data;
using System.Data.SqlClient;
using Dapper;
using Microsoft.SqlServer.Dac;

namespace StigsUtils.SqlServer;

public class SqlServerDbManager
{
  private readonly string _connectionString;
  private readonly SqlConnectionStringBuilder _connectionStringBuilder;
  private readonly string _masterConnectionString;
  private readonly SqlConnectionStringBuilder _masterConnectionStringBuilder;
  private string _workingDir = Environment.CurrentDirectory;
  public string DatabaseName
  {
    get => _connectionStringBuilder.InitialCatalog;
    set => _connectionStringBuilder.InitialCatalog = value;
  }

  public string DatabaseServer => _connectionStringBuilder.DataSource;

  public SqlServerDbManager(string connectionString) : this(new SqlConnectionStringBuilder(connectionString)) { }
  public SqlServerDbManager(SqlConnectionStringBuilder connectionStringBuilder)
  {
    _connectionStringBuilder = connectionStringBuilder;
    _connectionString = connectionStringBuilder.ToString();
    _masterConnectionStringBuilder = new SqlConnectionStringBuilder(_connectionString)
    {
      InitialCatalog = "master"
    };
    _masterConnectionString = _masterConnectionStringBuilder.ToString();
  }
  
  public string ConnectionString => _connectionString;
  public IDbConnection Open(string? connectionString = null)
  {
    connectionString ??= _connectionString;
    var connection = new SqlConnection(connectionString);
    connection.Open();
    return connection;
  }
  public IDbConnection OpenMaster() => Open(_masterConnectionString);

  public string ExportDatabase(string outputDirectory, Version? version = null, string appVersion = "AppVersion1")
  {
    if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);
    var databaseName = DatabaseName;
    version ??= new Version(1, 0, 0);
    var dacpacFile = Path.Combine(outputDirectory, databaseName + ".dacpac");
    var sqlFiles = Path.Combine(outputDirectory, databaseName);

    DacExtractOptions extractOptions = new DacExtractOptions
    {
      ExtractApplicationScopedObjectsOnly = true,    // Extracts only the objects that are defined in the user database, excluding system objects.
      IgnorePermissions = true,                      // Set to true if you do not want to extract permissions, otherwise false to include them.
      ExtractReferencedServerScopedElements = false, // Includes elements in the database that reference server-scoped elements, like logins.
      Storage = DacSchemaModelStorageType.Memory,    // Determines how the schema model is stored during extraction. Memory is usually sufficient.
      ExtractAllTableData = false,                   // Important: set to false to exclude table data.
      ExtractTarget = DacExtractTarget.DacPac,
      CommandTimeout = 60,
      LongRunningCommandTimeout = 0,
      DatabaseLockTimeout = 60,
      IgnoreExtendedProperties = true,
      IgnoreUserLoginMappings = true,
      VerifyExtraction = false // Verifies the extraction process. This can be useful to ensure the integrity of the extracted dacpac.
    };
    var dacServices = new DacServices(_connectionString);
    dacServices.Extract(dacpacFile, databaseName, appVersion, version, extractOptions: extractOptions);
    extractOptions.ExtractTarget = DacExtractTarget.SchemaObjectType;
    dacServices.Extract(sqlFiles, databaseName, appVersion, version, extractOptions: extractOptions);
    return outputDirectory;
  }

  // public static string CreateUpdateScript(string connectionString, string dacpacPath, string targetName)
  // {
  //   var sourceEndpoint = new SchemaCompareDatabaseEndpoint(connectionString);
  //   var targetEndpoint = new SchemaCompareDacpacEndpoint(dacpacPath);
  //   SchemaComparison comparison = new SchemaComparison(sourceEndpoint, targetEndpoint);
  //   SchemaComparisonResult? result = comparison.Compare();
  //   return result.GenerateScript(targetName).Script;
  // }

  public bool DatabaseExists(string? databaseName = null)
  {
    string sql = "SELECT CAST(CASE WHEN EXISTS(SELECT * FROM sys.databases WHERE name = @name) THEN 1 ELSE 0 END AS BIT)";
    using IDbConnection master = (SqlConnection)OpenMaster();
    var result = master.ExecuteScalar<bool>(sql, new { name = databaseName ?? DatabaseName});
    return result;
  }

  public SqlServerDbManager EnsureExists(string? databaseName = null)
  {
    if (!DatabaseExists(databaseName ?? DatabaseName))
    {
      CreateDatabase(databaseName);
    }
    return this;
  }

  public SqlServerDbManager CreateDatabase(string? databaseName = null)
  {
    string sql = $"CREATE DATABASE {databaseName ?? DatabaseName}";
    using IDbConnection connection = (SqlConnection)OpenMaster();
    connection.Execute(sql);
    return this;
  }

  public SqlServerDbManager DropDatabaseIfExists(string? databaseName = null) => DropDatabase(databaseName ?? DatabaseName, true);
  public SqlServerDbManager DropDatabase(string? databaseName = null, bool ifExists = false)
  {
    databaseName ??= DatabaseName;
    using IDbConnection master = (SqlConnection)OpenMaster();
    if (master.ExecuteScalar($"SELECT database_id FROM sys.databases WHERE Name = '{databaseName}';") == null)
    {
      if(!ifExists) throw new ArgumentException($"Database {databaseName} does not exist or no permissions to view it.");
      return this;
    }
    string dropDatabaseSql = $"""
                                  USE master;
                                  ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                                  DROP DATABASE [{databaseName}];
                              """;
    master.Execute(dropDatabaseSql);
    return this;
  }

  public static SqlServerDbManager UseLocalHostDb(string connectionString, string? dbName = null, int port = 1433)
  {
    var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
    connectionStringBuilder.DataSource = "localhost,"+port;
    //var connectionString = $"Server=localhost,1433;Database={dbName};User Id=sa;Password=padb3,2023;Connect Timeout=15;Encrypt=false;TrustServerCertificate=true";
    var dbManager = new SqlServerDbManager(connectionStringBuilder);
    // if (dropAndCreate)
    // {
    //   dbManager.DropDatabaseIfExists(dbName);
    // }
    // if (!dbManager.DatabaseExists(dbName))
    // {
    //   dbManager.CreateDatabaseFromScripts(dbName, sqlDir, addSeedDataIfCreatingDb);
    // }
    return dbManager;
  }

  public SqlServerDbManager SetWorkingDir(string dir)
  {
    _workingDir = Path.GetFullPath(dir);
    return this;
  }

  public SqlServerDbManager RunScripts(params string[] scripts)
  {
    EnsureExists();
    using ScriptRunner scriptRunner = new ScriptRunner(ConnectionString);
    foreach (var script in scripts)
    {
      scriptRunner.RunSql(script);
    }
    return this;
  }
  
  public SqlServerDbManager RunScriptFiles(params string[] scriptFiles)
  {
    EnsureExists();
    using ScriptRunner scriptRunner = new ScriptRunner(ConnectionString);
    foreach (var scriptFile in scriptFiles)
    {
      var path = Path.IsPathRooted(scriptFile) ? scriptFile : Path.Combine(_workingDir, scriptFile);
      scriptRunner.RunSqlFile(path);
    }
    return this;
  }
  
  public int Execute(string sql, object? args = null)
  {
    using var db = Open();
    return db.Execute(sql,args);
  }
  public T? ReadSingle<T>(string sql)
  {
    using var db = Open();
    return db.ExecuteScalar<T>(sql);
  }
}