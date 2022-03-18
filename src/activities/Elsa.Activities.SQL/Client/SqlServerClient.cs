using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
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

        private static DataSet ReadAsDataSet(SqlDataReader reader)
        {
            var dataSet = new DataSet("dataSet");

            var schemaTable = reader.GetSchemaTable();
            var data = new DataTable();
            dataSet.Tables.Add(data);

            foreach (DataRow row in schemaTable.Rows)
            {
                string colName = row.Field<string>("ColumnName");
                Type t = row.Field<Type>("DataType");
                data.Columns.Add(colName, t);
            }

            while (reader.Read())
            {
                var newRow = data.Rows.Add();
                foreach (DataColumn col in data.Columns)
                {
                    newRow[col.ColumnName] = reader[col.ColumnName];
                }
            }

            return dataSet;
        }
    }
}
