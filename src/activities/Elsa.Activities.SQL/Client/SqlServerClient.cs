using Microsoft.Data.SqlClient;

namespace Elsa.Activities.Sql.Client
{
    public class SqlServerClient : ISqlServerClient
    {
        private readonly string? _connectionString;
        public SqlServerClient(string connectionString)
        {
            _connectionString = connectionString;
        }

        public int Execute(string sqlCommand)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand(sqlCommand, connection);

                var result = command.ExecuteNonQuery();

                return result;
            }
        }
    }
}
