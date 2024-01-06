using Microsoft.Data.SqlClient;

namespace StigsUtils.SqlServer;

public class SqlConnectionString
{
  private readonly SqlConnectionStringBuilder _value;
  public SqlConnectionString(string value)
  {
    _value = new SqlConnectionStringBuilder(value);
  }
  public static implicit operator string(SqlConnectionString x) => x.ToString();
  public static implicit operator SqlConnectionString(string x) => new(x);
  public override string ToString() => _value.ToString();

  /// <summary>
  /// The "InitialCatalog" in SqlConnectionStringBuilder.
  /// </summary>
  public string DatabaseName
  {
    get => _value.InitialCatalog;
    set => _value.InitialCatalog = value;
  }
  /// <summary>
  /// The "DataSource" in SqlConnectionStringBuilder.
  /// </summary>
  public string Server
  {
    get => _value.DataSource;
    set => _value.DataSource = value;
  }
  public SqlConnectionString WithDatabaseName(string x)
  {
    DatabaseName = x;
    return this;
  }
  public SqlConnectionString WithServer(string x)
  {
    Server = x;
    return this;
  }

  public SqlConnectionString With(Action<SqlConnectionString> action)
  {
    action(this);
    return this;
  }

}