using System.Data.Common;
using Elsa.Sql.Client;
using Microsoft.Data.SqlClient;

namespace Elsa.Sql.SqlServer;

/// <summary>
/// Microsoft SQL server client implementation.
/// </summary>
/// <param name="connectionString"></param>
public class SqlServerClient(string connectionString) : BaseSqlClient(connectionString)
{
    public override string ParameterText { get; set; } = "p";

    protected override DbConnection CreateConnection() => new SqlConnection(_connectionString);

    protected override DbCommand CreateCommand(string query, DbConnection connection) => new SqlCommand(query, (SqlConnection)connection);
}