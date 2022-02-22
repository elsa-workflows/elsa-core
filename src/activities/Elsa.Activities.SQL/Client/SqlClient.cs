using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Elsa.Activities.SQL.Client
{
    public class SqlClient
    {
        private string _connectionString { get; set; }
        public SqlClient(string connectionString)
        {
            _connectionString = connectionString;
        }

        public int Execute(string sqlCommand)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand(sqlCommand, connection);

                var result = command.ExecuteNonQuery();

                return result;
            }
        }
    }
}
