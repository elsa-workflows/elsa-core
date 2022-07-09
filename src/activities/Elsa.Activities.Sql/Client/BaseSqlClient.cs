using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Activities.Sql.Client
{
    public abstract class BaseSqlClient
    {
        protected static DataSet ReadAsDataSet(IDataReader reader)
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
