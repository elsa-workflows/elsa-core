// Taken from https://www.codeproject.com/Tips/1147879/Print-DataTable-to-Console-and-more

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

 // ReSharper disable once CheckNamespace
 namespace System.Data
{
    public static class PrintDataExtensions
    {
        #region Print Columns

        public static void PrintColumns(this DataTable dataTable, params string[] columnNames)
        {
            PrintColumns(dataTable.TableName, GetColumns(dataTable, columnNames));
        }

        public static void PrintColumns(this DataView dataView, params string[] columnNames)
        {
            PrintColumns(dataView.Table.TableName, GetColumns(dataView.Table, columnNames));
        }

        public static void PrintColumns(this DataSet dataSet, params string[] columnNames)
        {
            foreach (DataTable dataTable in dataSet.Tables)
                PrintColumns(dataTable, columnNames);
        }

        private static void PrintColumns(string name, DataColumn[] columns)
        {
            string columnName = "Column Name";
            string dataType = "Data Type";
            string nullable = "Nullable";
            string dataMember = "Data Member";

            int length0 = 0;
            int length1 = columnName.Length;
            int length2 = dataType.Length;
            int length3 = nullable.Length;
            int length4 = dataMember.Length;

            if (columns.Length > 0)
            {
                int maxLength = columns.Select(c => c.Ordinal).Max().ToString().Length;
                if (length0 < maxLength)
                    length0 = maxLength;

                maxLength = columns.Select(c => c.ColumnName.Length).Max();
                if (length1 < maxLength)
                    length1 = maxLength;

                maxLength = columns.Select(c => c.DataType.ToString().Length).Max();
                if (length2 < maxLength)
                    length2 = maxLength;

                maxLength = columns.Select(c => GetDataMemberType(c).Length + 1 + c.ColumnName.Length).Max();
                if (length4 < maxLength)
                    length4 = maxLength;
            }

            string horizontal0 = new String(Horizontal_Bar, length0 + 2);
            string horizontal1 = new String(Horizontal_Bar, length1 + 2);
            string horizontal2 = new String(Horizontal_Bar, length2 + 2);
            string horizontal3 = new String(Horizontal_Bar, length3 + 2);
            string horizontal4 = new String(Horizontal_Bar, length4 + 2);

            if (string.IsNullOrEmpty(name) == false)
                WriteLine("{0}:", name);

            WriteLine("{5}{0}{6}{1}{6}{2}{6}{3}{6}{4}{7}", horizontal0, horizontal1, horizontal2, horizontal3, horizontal4, Top_Left, Top_Center, Top_Right);

            string format1 = string.Format("{{5}} {{0,-{0}}} {{5}} {{1,-{1}}} {{5}} {{2,-{2}}} {{5}} {{3,-{3}}} {{5}} {{4,-{4}}} {{5}}", length0, length1, length2, length3, length4);
            WriteLine(format1, string.Empty, columnName, dataType, nullable, dataMember, Verticl_Bar);

            WriteLine("{5}{0}{6}{1}{6}{2}{6}{3}{6}{4}{7}", horizontal0, horizontal1, horizontal2, horizontal3, horizontal4, Middle_Left, Middle_Center, Middle_Right);

            foreach (DataColumn column in columns)
            {
                string dataMemberType = GetDataMemberType(column);
                string format2 = string.Format("{{5}} {{0,{0}}} {{5}} {{1,-{1}}} {{5}} {{2,-{2}}} {{5}} {{3,-{3}}} {{5}} {{4}} {{1}} {{5,{4}}}", length0, length1, length2, length3, length4 - dataMemberType.Length - column.ColumnName.Length);
                WriteLine(format2, column.Ordinal, column.ColumnName, column.DataType, column.AllowDBNull, dataMemberType, Verticl_Bar);
            }

            WriteLine("{5}{0}{6}{1}{6}{2}{6}{3}{6}{4}{7}", horizontal0, horizontal1, horizontal2, horizontal3, horizontal4, Bottom_Left, Bottom_Center, Bottom_Right);

            WriteLine();
        }

        private static string GetDataMemberType(DataColumn column)
        {
            if (column.DataType == typeof(string))
                return "string";
            else if (column.DataType == typeof(int))
                return "int" + (column.AllowDBNull ? "?" : string.Empty);
            else if (column.DataType == typeof(short))
                return "short" + (column.AllowDBNull ? "?" : string.Empty);
            else if (column.DataType == typeof(long))
                return "long" + (column.AllowDBNull ? "?" : string.Empty);
            else if (column.DataType == typeof(double))
                return "double" + (column.AllowDBNull ? "?" : string.Empty);
            else if (column.DataType == typeof(decimal))
                return "decimal" + (column.AllowDBNull ? "?" : string.Empty);
            else if (column.DataType == typeof(float))
                return "float" + (column.AllowDBNull ? "?" : string.Empty);
            else if (column.DataType == typeof(char))
                return "char" + (column.AllowDBNull ? "?" : string.Empty);
            else if (column.DataType == typeof(bool))
                return "bool" + (column.AllowDBNull ? "?" : string.Empty);
            else if (column.DataType == typeof(uint))
                return "uint" + (column.AllowDBNull ? "?" : string.Empty);
            else if (column.DataType == typeof(ushort))
                return "ushort" + (column.AllowDBNull ? "?" : string.Empty);
            else if (column.DataType == typeof(ulong))
                return "ulong" + (column.AllowDBNull ? "?" : string.Empty);
            else if (column.DataType == typeof(byte))
                return "byte" + (column.AllowDBNull ? "?" : string.Empty);
            else if (column.DataType == typeof(sbyte))
                return "sbyte" + (column.AllowDBNull ? "?" : string.Empty);
            else if (column.DataType == typeof(DateTime))
                return "DateTime" + (column.AllowDBNull ? "?" : string.Empty);
            else if (column.DataType == typeof(TimeSpan))
                return "TimeSpan" + (column.AllowDBNull ? "?" : string.Empty);
            else if (column.DataType == typeof(Type))
                return "Type";
            else if (column.DataType == typeof(byte[]))
                return "byte[]";
            else
                return column.DataType.ToString() + (column.AllowDBNull && column.DataType.IsClass == false ? "?" : string.Empty);
        }

        #endregion

        #region Print DataTable

        public static void Print(this DataTable dataTable, params string[] columnNames)
        {
            Print(dataTable, false, 0, null, columnNames);
        }

        public static void Print(this DataTable dataTable, bool rowOrdinals, params string[] columnNames)
        {
            Print(dataTable, rowOrdinals, 0, null, columnNames);
        }

        public static void Print(this DataTable dataTable, int top, params string[] columnNames)
        {
            Print(dataTable, false, top, null, columnNames);
        }

        public static void Print(this DataTable dataTable, bool rowOrdinals, int top, params string[] columnNames)
        {
            Print(dataTable, rowOrdinals, top, null, columnNames);
        }

        public static void Print(this DataTable dataTable, ValueToStringHandler toString, params string[] columnNames)
        {
            Print(dataTable, false, 0, toString, columnNames);
        }

        public static void Print(this DataTable dataTable, bool rowOrdinals, ValueToStringHandler toString, params string[] columnNames)
        {
            Print(dataTable, rowOrdinals, 0, toString, columnNames);
        }

        public static void Print(this DataTable dataTable, int top, ValueToStringHandler toString, params string[] columnNames)
        {
            Print(dataTable, false, top, toString, columnNames);
        }

        public static void Print(this DataTable dataTable, bool rowOrdinals = false, int top = 0, ValueToStringHandler toString = null, params string[] columnNames)
        {
            PrintRows(dataTable, dataTable.AsEnumerable(), rowOrdinals, top, toString, columnNames);
        }

        #endregion

        #region Print DataView

        public static void Print(this DataView dataView, params string[] columnNames)
        {
            Print(dataView, false, 0, null, columnNames);
        }

        public static void Print(this DataView dataView, bool rowOrdinals, params string[] columnNames)
        {
            Print(dataView, rowOrdinals, 0, null, columnNames);
        }

        public static void Print(this DataView dataView, int top, params string[] columnNames)
        {
            Print(dataView, false, top, null, columnNames);
        }

        public static void Print(this DataView dataView, bool rowOrdinals, int top, params string[] columnNames)
        {
            Print(dataView, rowOrdinals, top, null, columnNames);
        }

        public static void Print(this DataView dataView, ValueToStringHandler toString, params string[] columnNames)
        {
            Print(dataView, false, 0, toString, columnNames);
        }

        public static void Print(this DataView dataView, bool rowOrdinals, ValueToStringHandler toString, params string[] columnNames)
        {
            Print(dataView, rowOrdinals, 0, toString, columnNames);
        }

        public static void Print(this DataView dataView, int top, ValueToStringHandler toString, params string[] columnNames)
        {
            Print(dataView, false, top, toString, columnNames);
        }

        public static void Print(this DataView dataView, bool rowOrdinals = false, int top = 0, ValueToStringHandler toString = null, params string[] columnNames)
        {
            PrintRows(dataView, rowOrdinals, top, toString, columnNames);
        }

        #endregion

        #region Print DataSet

        public static void Print(this DataSet dataSet, params string[] columnNames)
        {
            Print(dataSet, false, 0, null, columnNames);
        }

        public static void Print(this DataSet dataSet, bool rowOrdinals, params string[] columnNames)
        {
            Print(dataSet, rowOrdinals, 0, null, columnNames);
        }

        public static void Print(this DataSet dataSet, int top, params string[] columnNames)
        {
            Print(dataSet, false, top, null, columnNames);
        }

        public static void Print(this DataSet dataSet, bool rowOrdinals, int top, params string[] columnNames)
        {
            Print(dataSet, rowOrdinals, top, null, columnNames);
        }

        public static void Print(this DataSet dataSet, ValueToStringHandler toString, params string[] columnNames)
        {
            Print(dataSet, false, 0, toString, columnNames);
        }

        public static void Print(this DataSet dataSet, bool rowOrdinals, ValueToStringHandler toString, params string[] columnNames)
        {
            Print(dataSet, rowOrdinals, 0, toString, columnNames);
        }

        public static void Print(this DataSet dataSet, int top, ValueToStringHandler toString, params string[] columnNames)
        {
            Print(dataSet, false, top, toString, columnNames);
        }

        public static void Print(this DataSet dataSet, bool rowOrdinals = false, int top = 0, ValueToStringHandler toString = null, params string[] columnNames)
        {
            foreach (DataTable dataTable in dataSet.Tables)
                Print(dataTable, rowOrdinals, top, toString, columnNames);
        }

        #endregion

        #region Print DataRow[]

        public static void Print(this DataRow[] dataRows, params string[] columnNames)
        {
            Print(dataRows, false, 0, null, columnNames);
        }

        public static void Print(this DataRow[] dataRows, bool rowOrdinals, params string[] columnNames)
        {
            Print(dataRows, rowOrdinals, 0, null, columnNames);
        }

        public static void Print(this DataRow[] dataRows, int top, params string[] columnNames)
        {
            Print(dataRows, false, top, null, columnNames);
        }

        public static void Print(this DataRow[] dataRows, bool rowOrdinals, int top, params string[] columnNames)
        {
            Print(dataRows, rowOrdinals, top, null, columnNames);
        }

        public static void Print(this DataRow[] dataRows, ValueToStringHandler toString, params string[] columnNames)
        {
            Print(dataRows, false, 0, toString, columnNames);
        }

        public static void Print(this DataRow[] dataRows, bool rowOrdinals, ValueToStringHandler toString, params string[] columnNames)
        {
            Print(dataRows, rowOrdinals, 0, toString, columnNames);
        }

        public static void Print(this DataRow[] dataRows, int top, ValueToStringHandler toString, params string[] columnNames)
        {
            Print(dataRows, false, top, toString, columnNames);
        }

        public static void Print(this DataRow[] dataRows, bool rowOrdinals = false, int top = 0, ValueToStringHandler toString = null, params string[] columnNames)
        {
            PrintRows((dataRows.Length != 0 ? dataRows[0].Table : null), dataRows, rowOrdinals, top, toString, columnNames);
        }

        #endregion

        #region Print Helper Methods

        public delegate string ValueToStringHandler(object obj, DataRow row, DataColumn column);

        private static void PrintRows(DataTable dataTable, IEnumerable<DataRow> dataRows, bool rowOrdinals, int top, ValueToStringHandler toString, string[] columnNames, string ordinalColumnName = null)
        {
            if (dataTable == null && dataRows.Count() == 0)
            {
                WriteLine("No rows were selected");
                WriteLine();
                return;
            }

            if (dataTable != null && string.IsNullOrEmpty(dataTable.TableName) == false)
                WriteLine("{0}:", dataTable.TableName);

            DataColumn[] columns = GetColumns(dataTable, columnNames, ordinalColumnName);
            if (columns.Length == 0)
            {
                WriteLine("No columns were selected");
                WriteLine();
                return;
            }

            if (top > 0)
                dataRows = dataRows.Take(top);

            int[] lengths = columns.Select(c => c.ColumnName.Length).ToArray();
            foreach (DataRow row in dataRows)
                CalculateLengths(row, columns, lengths, toString);

            int rowOrdinalsLength = 0;
            if (rowOrdinals)
            {
                if (dataRows.Count() > 0)
                {
                    int maxRowOrdinal = 0;
                    if (string.IsNullOrEmpty(ordinalColumnName))
                        maxRowOrdinal = dataRows.Select(row => row.Table.Rows.IndexOf(row)).Max();
                    else
                        maxRowOrdinal = dataRows.Select(row => (int)row[ordinalColumnName]).Max();

                    if (maxRowOrdinal > -1)
                        rowOrdinalsLength = maxRowOrdinal.ToString().Length;
                }
            }

            string header = Top_Left.ToString();
            string separator = Middle_Left.ToString();
            string footer = Bottom_Left.ToString();
            string formatHeaders = Verticl_Bar.ToString();
            string format = Verticl_Bar.ToString();

            if (rowOrdinals)
            {
                string horizontal = new String(Horizontal_Bar, rowOrdinalsLength + 2);
                header += horizontal + Top_Center;
                separator += horizontal + Middle_Center;
                footer += horizontal + Bottom_Center;
                formatHeaders += string.Format(" {{0,-{0}}} {1}", rowOrdinalsLength, Verticl_Bar);
                format += string.Format(" {{0,{0}}} {1}", rowOrdinalsLength, Verticl_Bar);
            }

            int k = 0;
            for (; k < columns.Length - 1; k++)
            {
                string horizontal = new String(Horizontal_Bar, lengths[k] + 2);
                header += horizontal + Top_Center;
                separator += horizontal + Middle_Center;
                footer += horizontal + Bottom_Center;

                string cellFormat = string.Format(" {{{0},-{1}}} {2}", k + 1, lengths[k], Verticl_Bar);
                formatHeaders += cellFormat;
                format += cellFormat;
            }

            k = columns.Length - 1;
            if (k >= 0)
            {
                string horizontal = new String(Horizontal_Bar, lengths[k] + 2);
                header += horizontal + Top_Right;
                separator += horizontal + Middle_Right;
                footer += horizontal + Bottom_Right;

                string cellFormat = string.Format(" {{{0},-{1}}} {2}", k + 1, lengths[k], Verticl_Bar);
                formatHeaders += cellFormat;
                format += cellFormat;
            }

            object[] objects = new object[columns.Length + 1];

            WriteLine(header);

            objects[0] = string.Empty;
            for (int i = 0; i < columns.Length; i++)
                objects[i + 1] = columns[i];
            WriteLine(formatHeaders, objects);

            WriteLine(separator);

            foreach (DataRow row in dataRows)
            {
                if (rowOrdinals)
                {
                    int ordinal = 0;
                    if (string.IsNullOrEmpty(ordinalColumnName))
                        ordinal = row.Table.Rows.IndexOf(row);
                    else
                        ordinal = (int)row[ordinalColumnName];
                    objects[0] = (ordinal > -1 ? ordinal : (int?)null);
                }

                for (int i = 0; i < columns.Length; i++)
                {
                    object obj = row[columns[i]];

                    string str = null;
                    if (toString != null)
                    {
                        str = toString(obj, row, columns[i]);
                        if (str == null)
                            str = "null";
                    }
                    else
                    {
                        str = string.Format("{0}", (obj == DBNull.Value || obj == null ? "null" : obj));
                    }

                    objects[i + 1] = str;
                }

                WriteLine(format, objects);
            }

            WriteLine(footer);

            WriteLine();
        }

        private static void PrintRows(DataView dataView, bool rowOrdinals, int top, ValueToStringHandler toString, string[] columnNames)
        {
            string ordinalColumnName = null;
            DataTable dataTable = GetTableFromView(dataView, rowOrdinals, top, ref ordinalColumnName);
            PrintRows(dataTable, dataTable.AsEnumerable(), rowOrdinals, top, toString, columnNames, ordinalColumnName);
        }

        private static DataTable GetTableFromView(DataView dataView, bool rowOrdinals, int top, ref string ordinalColumnName)
        {
            DataTable dataTable = dataView.ToTable();

            if (rowOrdinals)
            {
                ordinalColumnName = "_ordinal";
                while (dataTable.Columns.Contains(ordinalColumnName))
                    ordinalColumnName = "_" + ordinalColumnName;
                dataTable.Columns.Add(ordinalColumnName, typeof(int));

                var it = dataView.GetEnumerator();
                int rowCounter = -1;
                while (it.MoveNext())
                {
                    rowCounter++;
                    if (top > 0 && rowCounter >= top)
                        break;
                    DataRow dataRow = ((DataRowView)it.Current).Row;
                    dataTable.Rows[rowCounter][ordinalColumnName] = dataRow.Table.Rows.IndexOf(dataRow);
                }
            }

            return dataTable;
        }

        private static DataColumn[] GetColumns(DataTable dataTable, string[] columnNames, string ordinalColumnName = null)
        {
            if (columnNames != null && columnNames.Length > 0)
                return columnNames.Join(dataTable.Columns.Cast<DataColumn>(), n => n, c => c.ColumnName, (n, c) => c, StringComparer.CurrentCultureIgnoreCase).Where(c => string.IsNullOrEmpty(ordinalColumnName) || c.ColumnName != ordinalColumnName).ToArray();
            else
                return dataTable.Columns.Cast<DataColumn>().Where(c => string.IsNullOrEmpty(ordinalColumnName) || c.ColumnName != ordinalColumnName).ToArray();
        }

        private static void CalculateLengths(DataRow row, DataColumn[] columns, int[] lengths, ValueToStringHandler toString)
        {
            for (int i = 0; i < columns.Length; i++)
            {
                object obj = row[columns[i]];

                string str = null;
                if (toString != null)
                {
                    str = toString(obj, row, columns[i]);
                    if (str == null)
                        str = "null";
                }
                else
                {
                    str = string.Format("{0}", (obj == DBNull.Value || obj == null ? "null" : obj));
                }

                if (lengths[i] < str.Length)
                    lengths[i] = str.Length;
            }
        }

        #endregion

        #region PrintList DataTable

        public static void PrintList(this DataTable dataTable, params string[] columnNames)
        {
            PrintList(dataTable, false, 0, null, columnNames: columnNames);
        }

        public static void PrintList(this DataTable dataTable, bool rowOrdinals, params string[] columnNames)
        {
            PrintList(dataTable, rowOrdinals, 0, null, columnNames: columnNames);
        }

        public static void PrintList(this DataTable dataTable, int top, params string[] columnNames)
        {
            PrintList(dataTable, false, top, null, columnNames: columnNames);
        }

        public static void PrintList(this DataTable dataTable, bool rowOrdinals, int top, params string[] columnNames)
        {
            PrintList(dataTable, rowOrdinals, top, null, columnNames: columnNames);
        }

        public static void PrintList(this DataTable dataTable, ValueToStringHandler toString, params string[] columnNames)
        {
            PrintList(dataTable, false, 0, toString, columnNames: columnNames);
        }

        public static void PrintList(this DataTable dataTable, bool rowOrdinals, ValueToStringHandler toString, params string[] columnNames)
        {
            PrintList(dataTable, rowOrdinals, 0, toString, columnNames: columnNames);
        }

        public static void PrintList(this DataTable dataTable, int top, ValueToStringHandler toString, params string[] columnNames)
        {
            PrintList(dataTable, false, top, toString, columnNames: columnNames);
        }

        public static void PrintList(this DataTable dataTable, int repeatColumns, RepeatDirection repeatDirection, params string[] columnNames)
        {
            PrintList(dataTable, false, 0, null, repeatColumns: repeatColumns, repeatDirection: repeatDirection, columnNames: columnNames);
        }

        public static void PrintList(this DataTable dataTable, bool rowOrdinals = false, int top = 0, ValueToStringHandler toString = null, int repeatColumns = 2, RepeatDirection repeatDirection = RepeatDirection.Vertical, string delimiter = ": ", params string[] columnNames)
        {
            PrintListRows(dataTable, dataTable.AsEnumerable(), rowOrdinals, top, toString, repeatColumns, repeatDirection, delimiter, columnNames);
        }

        #endregion

        #region PrintList DataView

        public static void PrintList(this DataView dataView, params string[] columnNames)
        {
            PrintList(dataView, false, 0, null, columnNames: columnNames);
        }

        public static void PrintList(this DataView dataView, bool rowOrdinals, params string[] columnNames)
        {
            PrintList(dataView, rowOrdinals, 0, null, columnNames: columnNames);
        }

        public static void PrintList(this DataView dataView, int top, params string[] columnNames)
        {
            PrintList(dataView, false, top, null, columnNames: columnNames);
        }

        public static void PrintList(this DataView dataView, bool rowOrdinals, int top, params string[] columnNames)
        {
            PrintList(dataView, rowOrdinals, top, null, columnNames: columnNames);
        }

        public static void PrintList(this DataView dataView, ValueToStringHandler toString, params string[] columnNames)
        {
            PrintList(dataView, false, 0, toString, columnNames: columnNames);
        }

        public static void PrintList(this DataView dataView, bool rowOrdinals, ValueToStringHandler toString, params string[] columnNames)
        {
            PrintList(dataView, rowOrdinals, 0, toString, columnNames: columnNames);
        }

        public static void PrintList(this DataView dataView, int top, ValueToStringHandler toString, params string[] columnNames)
        {
            PrintList(dataView, false, top, toString, columnNames: columnNames);
        }

        public static void PrintList(this DataView dataView, int repeatColumns, RepeatDirection repeatDirection, params string[] columnNames)
        {
            PrintList(dataView, false, 0, null, repeatColumns: repeatColumns, repeatDirection: repeatDirection, columnNames: columnNames);
        }

        public static void PrintList(this DataView dataView, bool rowOrdinals = false, int top = 0, ValueToStringHandler toString = null, int repeatColumns = 2, RepeatDirection repeatDirection = RepeatDirection.Vertical, string delimiter = ": ", params string[] columnNames)
        {
            PrintListRows(dataView, rowOrdinals, top, toString, repeatColumns, repeatDirection, delimiter, columnNames);
        }

        #endregion

        #region PrintList DataSet

        public static void PrintList(this DataSet dataSet, params string[] columnNames)
        {
            PrintList(dataSet, false, 0, null, columnNames: columnNames);
        }

        public static void PrintList(this DataSet dataSet, bool rowOrdinals, params string[] columnNames)
        {
            PrintList(dataSet, rowOrdinals, 0, null, columnNames: columnNames);
        }

        public static void PrintList(this DataSet dataSet, int top, params string[] columnNames)
        {
            PrintList(dataSet, false, top, null, columnNames: columnNames);
        }

        public static void PrintList(this DataSet dataSet, bool rowOrdinals, int top, params string[] columnNames)
        {
            PrintList(dataSet, rowOrdinals, top, null, columnNames: columnNames);
        }

        public static void PrintList(this DataSet dataSet, ValueToStringHandler toString, params string[] columnNames)
        {
            PrintList(dataSet, false, 0, toString, columnNames: columnNames);
        }

        public static void PrintList(this DataSet dataSet, bool rowOrdinals, ValueToStringHandler toString, params string[] columnNames)
        {
            PrintList(dataSet, rowOrdinals, 0, toString, columnNames: columnNames);
        }

        public static void PrintList(this DataSet dataSet, int top, ValueToStringHandler toString, params string[] columnNames)
        {
            PrintList(dataSet, false, top, toString, columnNames: columnNames);
        }

        public static void PrintList(this DataSet dataSet, int repeatColumns, RepeatDirection repeatDirection, params string[] columnNames)
        {
            PrintList(dataSet, false, 0, null, repeatColumns: repeatColumns, repeatDirection: repeatDirection, columnNames: columnNames);
        }

        public static void PrintList(this DataSet dataSet, bool rowOrdinals = false, int top = 0, ValueToStringHandler toString = null, int repeatColumns = 2, RepeatDirection repeatDirection = RepeatDirection.Vertical, string delimiter = ": ", params string[] columnNames)
        {
            foreach (DataTable dataTable in dataSet.Tables)
                PrintList(dataTable, rowOrdinals, top, toString, repeatColumns, repeatDirection, delimiter, columnNames);
        }

        #endregion

        #region PrintList DataRow[]

        public static void PrintList(this DataRow[] dataRows, params string[] columnNames)
        {
            PrintList(dataRows, false, 0, null, columnNames: columnNames);
        }

        public static void PrintList(this DataRow[] dataRows, bool rowOrdinals, params string[] columnNames)
        {
            PrintList(dataRows, rowOrdinals, 0, null, columnNames: columnNames);
        }

        public static void PrintList(this DataRow[] dataRows, int top, params string[] columnNames)
        {
            PrintList(dataRows, false, top, null, columnNames: columnNames);
        }

        public static void PrintList(this DataRow[] dataRows, bool rowOrdinals, int top, params string[] columnNames)
        {
            PrintList(dataRows, rowOrdinals, top, null, columnNames: columnNames);
        }

        public static void PrintList(this DataRow[] dataRows, ValueToStringHandler toString, params string[] columnNames)
        {
            PrintList(dataRows, false, 0, toString, columnNames: columnNames);
        }

        public static void PrintList(this DataRow[] dataRows, bool rowOrdinals, ValueToStringHandler toString, params string[] columnNames)
        {
            PrintList(dataRows, rowOrdinals, 0, toString, columnNames: columnNames);
        }

        public static void PrintList(this DataRow[] dataRows, int top, ValueToStringHandler toString, params string[] columnNames)
        {
            PrintList(dataRows, false, top, toString, columnNames: columnNames);
        }

        public static void PrintList(this DataRow[] dataRows, int repeatColumns, RepeatDirection repeatDirection, params string[] columnNames)
        {
            PrintList(dataRows, false, 0, null, repeatColumns: repeatColumns, repeatDirection: repeatDirection, columnNames: columnNames);
        }

        public static void PrintList(this DataRow[] dataRows, bool rowOrdinals = false, int top = 0, ValueToStringHandler toString = null, int repeatColumns = 2, RepeatDirection repeatDirection = RepeatDirection.Vertical, string delimiter = ": ", params string[] columnNames)
        {
            PrintListRows((dataRows.Length != 0 ? dataRows[0].Table : null), dataRows, rowOrdinals, top, toString, repeatColumns, repeatDirection, delimiter, columnNames);
        }

        #endregion

        #region PrintList Helper Methods

        public enum RepeatDirection
        {
            Horizontal = 0,
            Vertical = 1,
        }

        private static void PrintListRows(DataTable dataTable, IEnumerable<DataRow> dataRows, bool rowOrdinals, int top, ValueToStringHandler toString, int repeatColumns, RepeatDirection repeatDirection, string delimiter, string[] columnNames, string ordinalColumnName = null)
        {
            if (dataTable != null && string.IsNullOrEmpty(dataTable.TableName) == false)
                WriteLine("{0}:", dataTable.TableName);

            if (dataRows.Count() == 0)
            {
                WriteLine("No rows were selected");
                WriteLine();
                return;
            }

            DataColumn[] columns = GetColumns(dataTable, columnNames, ordinalColumnName);
            if (columns.Length == 0)
            {
                WriteLine("No columns were selected");
                WriteLine();
                return;
            }

            if (top > 0)
                dataRows = dataRows.Take(top);

            int columnsLength = columns.Select(c => c.ColumnName.Length).Max();

            int[] lengths = new int[columns.Length];
            foreach (DataRow row in dataRows)
                CalculateLengths(row, columns, lengths, toString);
            int rowsLength = lengths.Max();

            if (rowOrdinals)
            {
                if (dataRows.Count() > 0)
                {
                    if (columnsLength < 7) // "Ordinal".Length
                        columnsLength = 7;

                    int maxRowOrdinal = 0;
                    if (string.IsNullOrEmpty(ordinalColumnName))
                        maxRowOrdinal = dataRows.Select(row => row.Table.Rows.IndexOf(row)).Max();
                    else
                        maxRowOrdinal = dataRows.Select(row => (int)row[ordinalColumnName]).Max();

                    if (maxRowOrdinal > -1)
                    {
                        int rowOrdinalsLength = maxRowOrdinal.ToString().Length;
                        if (rowsLength < rowOrdinalsLength)
                            rowsLength = rowOrdinalsLength;
                    }
                }
            }

            if (repeatColumns < 1)
                repeatColumns = 1;

            if (repeatColumns > dataRows.Count())
                repeatColumns = dataRows.Count();

            int lastRowFilledCellsCount = dataRows.Count() % repeatColumns;
            if (lastRowFilledCellsCount == 0)
                lastRowFilledCellsCount = repeatColumns;
            int lastRowEmptyCellsCount = repeatColumns - lastRowFilledCellsCount;
            int rowsCount = (dataRows.Count() / repeatColumns) + (dataRows.Count() % repeatColumns > 0 ? 1 : 0);

            if (delimiter == null)
                delimiter = string.Empty;

            string cellFormat = string.Format(" {{{{0,-{0}}}}}{1}{{{{{{0}},-{2}}}}} {3}", columnsLength, delimiter, rowsLength, Verticl_Bar);
            string horizontal = new String(Horizontal_Bar, columnsLength + delimiter.Length + rowsLength + 2);

            string header = Top_Left.ToString();
            string separator = Middle_Left.ToString();
            string footer = Bottom_Left.ToString();
            string format = Verticl_Bar.ToString();

            int k = 0;
            for (; k < repeatColumns - 1; k++)
            {
                header += horizontal + Top_Center;
                separator += horizontal + Middle_Center;
                footer += horizontal + Bottom_Center;
                format += string.Format(cellFormat, k + 1);
            }

            k = repeatColumns - 1;
            if (k >= 0)
            {
                header += horizontal + Top_Right;
                separator += horizontal + Middle_Right;
                footer += horizontal + Bottom_Right;
                format += string.Format(cellFormat, k + 1);
            }

            string formatPartial = format;
            if (lastRowEmptyCellsCount > 0)
            {
                formatPartial = Verticl_Bar.ToString();
                for (int i = 0; i < lastRowFilledCellsCount; i++)
                    formatPartial += string.Format(cellFormat, i + 1);
                for (int i = 0; i < lastRowEmptyCellsCount; i++)
                    formatPartial += new String(' ', columnsLength + delimiter.Length + rowsLength + 2) + Verticl_Bar;
            }

            object[] objects = new object[repeatColumns + 1];

            if (repeatDirection == RepeatDirection.Horizontal)
            {
                for (int i = 0; i < rowsCount; i++)
                {
                    WriteLine(i == 0 ? header : separator);
                    var rows = dataRows.Skip(i * repeatColumns).Take(repeatColumns);
                    PrintListRow(rows, columns, objects, (i < rowsCount - 1 ? format : formatPartial), rowOrdinals, toString, ordinalColumnName);
                }
            }
            else if (repeatDirection == RepeatDirection.Vertical && lastRowEmptyCellsCount == 0)
            {
                for (int i = 0; i < rowsCount; i++)
                {
                    WriteLine(i == 0 ? header : separator);
                    var rows = dataRows.Where((r, n) => n % rowsCount == i);
                    PrintListRow(rows, columns, objects, (i < rowsCount - 1 ? format : formatPartial), rowOrdinals, toString, ordinalColumnName);
                }
            }
            else if (repeatDirection == RepeatDirection.Vertical && lastRowEmptyCellsCount > 0)
            {
                for (int i = 0; i < rowsCount; i++)
                {
                    WriteLine(i == 0 ? header : separator);
                    var rows = dataRows.Where((r, n) =>
                        (n < lastRowFilledCellsCount * rowsCount && n % rowsCount == i) ||
                        (n >= lastRowFilledCellsCount * rowsCount && (n - (lastRowFilledCellsCount * rowsCount)) % (rowsCount - 1) == i)
                    );
                    PrintListRow(rows, columns, objects, (i < rowsCount - 1 ? format : formatPartial), rowOrdinals, toString, ordinalColumnName);
                }
            }

            WriteLine(footer);

            WriteLine();
        }

        private static void PrintListRows(DataView dataView, bool rowOrdinals, int top, ValueToStringHandler toString, int repeatColumns, RepeatDirection repeatDirection, string delimiter, string[] columnNames)
        {
            string ordinalColumnName = null;
            DataTable dataTable = GetTableFromView(dataView, rowOrdinals, top, ref ordinalColumnName);
            PrintListRows(dataTable, dataTable.AsEnumerable(), rowOrdinals, top, toString, repeatColumns, repeatDirection, delimiter, columnNames, ordinalColumnName);
        }

        private static void PrintListRow(IEnumerable<DataRow> rows, DataColumn[] columns, object[] objects, string format, bool rowOrdinals, ValueToStringHandler toString, string ordinalColumnName)
        {
            if (rowOrdinals)
            {
                IEnumerable<int> ordinals = null;
                if (string.IsNullOrEmpty(ordinalColumnName))
                    ordinals = rows.Select(row => row.Table.Rows.IndexOf(row));
                else
                    ordinals = rows.Select(row => (int)row[ordinalColumnName]);

                objects[0] = "Ordinal";
                int k = 1;
                foreach (int ordinal in ordinals)
                    objects[k++] = (ordinal > -1 ? ordinal : (int?)null);
                WriteLine(format, objects);
            }

            for (int i = 0; i < columns.Length; i++)
            {
                objects[0] = columns[i].ColumnName;

                int k = 1;
                foreach (DataRow row in rows)
                {
                    object obj = row[columns[i]];

                    string str = null;
                    if (toString != null)
                    {
                        str = toString(obj, row, columns[i]);
                        if (str == null)
                            str = "null";
                    }
                    else
                    {
                        str = string.Format("{0}", (obj == DBNull.Value || obj == null ? "null" : obj));
                    }

                    objects[k++] = str;
                }

                WriteLine(format, objects);
            }
        }

        #endregion

        #region Write, WriteLine

        public delegate void WriteHandler(string value = null, params object[] args);
        public delegate void WriteLineHandler(string value = null, params object[] args);

        public static event WriteHandler Write = ConsoleWrite;
        public static event WriteLineHandler WriteLine = ConsoleWriteLine;

        public static void SetOutput(WriteHandler writeHandler, WriteLineHandler writeLineHandler)
        {
            Write = null;
            WriteLine = null;
            Write += writeHandler;
            WriteLine += writeLineHandler;
        }

        public static void SetOutputConsole()
        {
            SetOutput(ConsoleWrite, ConsoleWriteLine);
        }

        public static void SetOutputStringBuilder(StringBuilder builder)
        {
            SetOutput(
                (value, args) => StringBuilderWrite(builder, value, args),
                (value, args) => StringBuilderWriteLine(builder, value, args)
            );
        }

        public static void SetOutputStream(Stream stream)
        {
            SetOutputStream(stream, Encoding.UTF8);
        }

        public static void SetOutputStream(Stream stream, Encoding encoding)
        {
            SetOutput(
                (value, args) => StreamWrite(stream, encoding ?? Encoding.UTF8, value, args),
                (value, args) => StreamWriteLine(stream, encoding ?? Encoding.UTF8, value, args)
            );
        }

        #region Console

        private static void ConsoleWrite(string value = null, params object[] args)
        {
            if (string.IsNullOrEmpty(value))
                return;

            if (args == null)
                Console.Write(value);
            else
                Console.Write(value, args);
        }

        private static void ConsoleWriteLine(string value = null, params object[] args)
        {
            if (string.IsNullOrEmpty(value))
                Console.WriteLine();
            else if (args == null)
                Console.WriteLine(value);
            else
                Console.WriteLine(value, args);
        }

        #endregion

        #region StringBuilder

        private static void StringBuilderWrite(StringBuilder builder, string value = null, params object[] args)
        {
            if (string.IsNullOrEmpty(value))
                return;

            if (args == null)
                builder.Append(value);
            else
                builder.AppendFormat(value, args);
        }

        private static void StringBuilderWriteLine(StringBuilder builder, string value = null, params object[] args)
        {
            if (string.IsNullOrEmpty(value))
            {
                builder.AppendLine();
            }
            else if (args == null)
            {
                builder.AppendLine(value);
            }
            else
            {
                builder.AppendFormat(value, args);
                builder.AppendLine();
            }
        }

        #endregion

        #region Stream

        private static void StreamWrite(Stream stream, Encoding encoding, string value = null, params object[] args)
        {
            if (string.IsNullOrEmpty(value))
                return;

            byte[] buffer = null;

            if (args == null)
                buffer = encoding.GetBytes(value);
            else
                buffer = encoding.GetBytes(string.Format(value, args));

            stream.Write(buffer, 0, buffer.Length);
        }

        private static void StreamWriteLine(Stream stream, Encoding encoding, string value = null, params object[] args)
        {
            byte[] buffer = null;

            if (string.IsNullOrEmpty(value) == false)
            {
                if (args == null)
                    buffer = encoding.GetBytes(value);
                else
                    buffer = encoding.GetBytes(string.Format(value, args));
                stream.Write(buffer, 0, buffer.Length);
            }

            buffer = encoding.GetBytes(Environment.NewLine);
            stream.Write(buffer, 0, buffer.Length);
        }

        #endregion

        #endregion

        #region Border

        private const char ASCII_MINUS = '-';
        private const char ASCII_VERTICL_BAR = '|';
        private const char ASCII_PLUS = '+';
        private const char EXTENDED_ASCII_HORIZONTAL_BAR = '─';
        private const char EXTENDED_ASCII_VERTICL_BAR = '│';
        private const char EXTENDED_ASCII_TOP_LEFT = '┌';
        private const char EXTENDED_ASCII_TOP_CENTER = '┬';
        private const char EXTENDED_ASCII_TOP_RIGHT = '┐';
        private const char EXTENDED_ASCII_MIDDLE_LEFT = '├';
        private const char EXTENDED_ASCII_MIDDLE_CENTER = '┼';
        private const char EXTENDED_ASCII_MIDDLE_RIGHT = '┤';
        private const char EXTENDED_ASCII_BOTTOM_LEFT = '└';
        private const char EXTENDED_ASCII_BOTTOM_CENTER = '┴';
        private const char EXTENDED_ASCII_BOTTOM_RIGHT = '┘';

        private static char Horizontal_Bar;
        private static char Verticl_Bar;
        private static char Top_Left;
        private static char Top_Center;
        private static char Top_Right;
        private static char Middle_Left;
        private static char Middle_Center;
        private static char Middle_Right;
        private static char Bottom_Left;
        private static char Bottom_Center;
        private static char Bottom_Right;

        public static void ClearBorder()
        {
            Horizontal_Bar = ' ';
            Verticl_Bar = ' ';
            Top_Left = ' ';
            Top_Center = ' ';
            Top_Right = ' ';
            Middle_Left = ' ';
            Middle_Center = ' ';
            Middle_Right = ' ';
            Bottom_Left = ' ';
            Bottom_Center = ' ';
            Bottom_Right = ' ';
        }

        public static void ASCIIBorder()
        {
            Horizontal_Bar = ASCII_MINUS;
            Verticl_Bar = ASCII_VERTICL_BAR;
            Top_Left = ASCII_PLUS;
            Top_Center = ASCII_PLUS;
            Top_Right = ASCII_PLUS;
            Middle_Left = ASCII_PLUS;
            Middle_Center = ASCII_PLUS;
            Middle_Right = ASCII_PLUS;
            Bottom_Left = ASCII_PLUS;
            Bottom_Center = ASCII_PLUS;
            Bottom_Right = ASCII_PLUS;
        }

        public static void ExtendedASCIIBorder()
        {
            Horizontal_Bar = EXTENDED_ASCII_HORIZONTAL_BAR;
            Verticl_Bar = EXTENDED_ASCII_VERTICL_BAR;
            Top_Left = EXTENDED_ASCII_TOP_LEFT;
            Top_Center = EXTENDED_ASCII_TOP_CENTER;
            Top_Right = EXTENDED_ASCII_TOP_RIGHT;
            Middle_Left = EXTENDED_ASCII_MIDDLE_LEFT;
            Middle_Center = EXTENDED_ASCII_MIDDLE_CENTER;
            Middle_Right = EXTENDED_ASCII_MIDDLE_RIGHT;
            Bottom_Left = EXTENDED_ASCII_BOTTOM_LEFT;
            Bottom_Center = EXTENDED_ASCII_BOTTOM_CENTER;
            Bottom_Right = EXTENDED_ASCII_BOTTOM_RIGHT;
        }

        static PrintDataExtensions()
        {
            ExtendedASCIIBorder();
        }

        #endregion
    }
}