using Microsoft.Data.SqlClient;
using System.Data;

namespace Elsa.Activities.Sql.Client
{
    public class SqlServerClient : BaseSqlClient, ISqlClient
    {
        private readonly string? _connectionString;
        public SqlServerClient(string connectionString)
        {
            _connectionString = connectionString;
        }

        public int ExecuteCommand(string sqlCommand)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand(sqlCommand, connection);

                var result = command.ExecuteNonQuery();

                return result;
            }
        }

        public DataSet ExecuteQuery(string sqlQuery)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand(sqlQuery, connection);

                using (var reader = command.ExecuteReader())
                {
                    return ReadAsDataSet(reader);
                }
            }
        }

    }
}
