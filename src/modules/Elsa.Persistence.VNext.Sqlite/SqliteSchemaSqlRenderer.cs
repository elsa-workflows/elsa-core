using System.Text;
using Elsa.Persistence.VNext.Contracts;
using Elsa.Persistence.VNext.Relational;

namespace Elsa.Persistence.VNext.Sqlite;

public class SqliteSchemaSqlRenderer : IPersistenceSchemaRenderer<RelationalSchemaPlan>
{
    public IReadOnlyList<string> Render(RelationalSchemaPlan plan)
    {
        var statements = new List<string>();
        statements.AddRange(plan.Tables.Select(RenderCreateTable));
        statements.AddRange(plan.Indexes.Select(RenderCreateIndex));
        return statements;
    }

    private static string RenderCreateTable(RelationalTable table)
    {
        var builder = new StringBuilder();
        builder.Append("CREATE TABLE IF NOT EXISTS ");
        builder.Append(Quote(table.Name));
        builder.AppendLine(" (");

        var definitions = table.Columns
            .Select(column => $"    {RenderColumn(column)}")
            .ToList();

        if (table.PrimaryKey is not null)
            definitions.Add($"    CONSTRAINT {Quote(table.PrimaryKey.Name)} PRIMARY KEY ({RenderColumnList(table.PrimaryKey.Columns)})");

        builder.AppendLine(string.Join("," + Environment.NewLine, definitions));
        builder.Append(");");
        return builder.ToString();
    }

    private static string RenderCreateIndex(RelationalIndex index)
    {
        var unique = index.IsUnique ? "UNIQUE " : "";
        return $"CREATE {unique}INDEX IF NOT EXISTS {Quote(index.Name)} ON {Quote(index.Table)} ({RenderColumnList(index.Columns)});";
    }

    private static string RenderColumn(RelationalColumn column)
    {
        var nullability = column.IsNullable ? "" : " NOT NULL";
        return $"{Quote(column.Name)} {column.StoreType}{nullability}";
    }

    private static string RenderColumnList(IEnumerable<string> columns)
    {
        return string.Join(", ", columns.Select(Quote));
    }

    private static string Quote(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
