using Npgsql;
using Elsa.Sql.Client;
using System.Data.Common;

namespace Elsa.Sql.PostgreSql;

/// <summary>
/// PostgreSQL client implementation.
/// </summary>
/// <param name="connectionString"></param>
public class PostgreSqlClient(string connectionString) : BaseSqlClient(connectionString)
{
    protected override DbConnection CreateConnection() => new NpgsqlConnection(_connectionString);

    protected override DbCommand CreateCommand(string query, DbConnection connection) => new NpgsqlCommand(query, (NpgsqlConnection)connection);
}