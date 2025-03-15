using System.Data.Common;
using Elsa.Sql.Client;
using Microsoft.Data.Sqlite;

namespace Elsa.Sql.Sqlite;

/// <summary>
/// Sqlite client implementation.
/// </summary>
/// <param name="connectionString"></param>
public class SqliteClient(string connectionString) : BaseSqlClient(connectionString)
{
    public override string ParameterText { get; set; } = "p";

    protected override DbConnection CreateConnection() => new SqliteConnection(_connectionString);

    protected override DbCommand CreateCommand(string query, DbConnection connection) => new SqliteCommand(query, (SqliteConnection)connection);
}