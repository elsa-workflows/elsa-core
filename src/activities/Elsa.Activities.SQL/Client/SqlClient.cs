using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Activities.Sql.Client
{
    public class SqlClient
    {
        private readonly string? _connectionString;
        public SqlClient(string connectionString)
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
