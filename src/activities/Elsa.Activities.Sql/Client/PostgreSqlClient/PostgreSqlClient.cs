using Npgsql;
using System.Data;

namespace Elsa.Activities.Sql.Client.PostgreSqlClient
{
    public class PostgreSqlClient : BaseSqlClient, ISqlClient
    {
        private readonly string? _connectionString;
        public PostgreSqlClient(string connectionString)
        {
            _connectionString = connectionString;
        }

        public int ExecuteCommand(string sqlCommand)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();
                var command = new NpgsqlCommand(sqlCommand, connection);

                var result = command.ExecuteNonQuery();

                return result;
            }
        }

        public DataSet ExecuteQuery(string sqlQuery)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();
                var command = new NpgsqlCommand(sqlQuery, connection);

                using (var reader = command.ExecuteReader())
                {
                    return ReadAsDataSet(reader);
                }
            }
        }
    }
}
