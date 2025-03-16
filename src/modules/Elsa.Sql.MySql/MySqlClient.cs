using MySql.Data.MySqlClient;
using Elsa.Sql.Client;
using System.Data.Common;

namespace Elsa.Sql.MySql;

/// <summary>
/// MySql client implementation.
/// </summary>
/// <param name="connectionString"></param>
public class MySqlClient(string connectionString) : BaseSqlClient(connectionString)
{
    protected override DbConnection CreateConnection() => new MySqlConnection(_connectionString);

    protected override DbCommand CreateCommand(string query, DbConnection connection) => new MySqlCommand(query, (MySqlConnection)connection);
}