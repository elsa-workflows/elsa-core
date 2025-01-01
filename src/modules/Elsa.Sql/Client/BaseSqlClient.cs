using System.Data;

namespace Elsa.Sql.Client;

public abstract class BaseSqlClient
{
    /// <summary>
    /// Returns <see cref="IDataReader"/> data as a <see cref="DataSet"/>.
    /// </summary>
    /// <param name="reader">Reader to return data from.</param>
    /// <returns><see cref="DataSet"/> of data.</returns>
    protected static DataSet ReadAsDataSet(IDataReader reader)
    {
        var dataSet  = new DataSet("dataset");

        var schematable = reader.GetSchemaTable();
        var data = new DataSet();
        dataSet.Tables.Add(ReadAsDataTable(reader));
        
        return dataSet;
    }

    /// <summary>
    /// Returns <see cref="IDataReader"/> data as a <see cref="DataTable"/>.
    /// </summary>
    /// <param name="reader">Reader to return data from.</param>
    /// <returns><see cref="DataTable"/> of data.</returns>
    protected static DataTable ReadAsDataTable(IDataReader reader)
    {
        var data = new DataTable();
        var schemaTable =reader.GetSchemaTable();

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
        return data;
    }
}