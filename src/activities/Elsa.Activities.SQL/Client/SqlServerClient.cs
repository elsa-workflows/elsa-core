using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Text;

namespace Elsa.Activities.Sql.Client
{
    public class SqlServerClient : ISqlServerClient
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

        public string ExecuteQuery(string sqlQuery)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand(sqlQuery, connection);

                using (var reader = command.ExecuteReader())
                {
                    return ReadRows(reader);
                }
            }
        }

        private static string ReadRows(SqlDataReader reader)
        {
            StringBuilder sb = new StringBuilder();
            if (reader.HasRows)
            {
                if (sb.Length > 0) sb.Append("___");

                while (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                        if (reader.GetValue(i) != DBNull.Value)
                            sb.AppendFormat("{0}-", Convert.ToString(reader.GetValue(i)));
                }
            }

            return sb.ToString();
        }
    }
}
