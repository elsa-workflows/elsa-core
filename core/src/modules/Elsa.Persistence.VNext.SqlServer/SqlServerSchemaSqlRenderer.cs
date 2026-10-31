using System.Text;
using Elsa.Persistence.VNext.Contracts;
using Elsa.Persistence.VNext.Relational;

namespace Elsa.Persistence.VNext.SqlServer;

public class SqlServerSchemaSqlRenderer : IPersistenceSchemaRenderer<RelationalSchemaPlan>
{
    public IReadOnlyList<string> Render(RelationalSchemaPlan plan)
    {
        var statements = new List<string>();
        statements.AddRange(plan.Tables.SelectMany(RenderCreateTable));
        statements.AddRange(plan.Indexes.Select(RenderCreateIndex));
        return statements;
    }

    private static IEnumerable<string> RenderCreateTable(RelationalTable table)
    {
        if (!string.IsNullOrWhiteSpace(table.Schema))
            yield return $"IF SCHEMA_ID(N{QuoteStringLiteral(table.Schema)}) IS NULL EXEC(N'CREATE SCHEMA {Quote(table.Schema)}');";

        var builder = new StringBuilder();
        builder.Append("IF OBJECT_ID(N");
        builder.Append(QuoteStringLiteral(QualifiedName(table)));
        builder.AppendLine(", N'U') IS NULL");
        builder.AppendLine("BEGIN");
        builder.Append("    CREATE TABLE ");
        builder.Append(QualifiedName(table));
        builder.AppendLine(" (");

        var definitions = table.Columns
            .Select(column => $"        {RenderColumn(column)}")
            .ToList();

        if (table.PrimaryKey is not null)
            definitions.Add($"        CONSTRAINT {Quote(table.PrimaryKey.Name)} PRIMARY KEY ({RenderColumnList(table.PrimaryKey.Columns)})");

        builder.AppendLine(string.Join("," + Environment.NewLine, definitions));
        builder.AppendLine("    );");
        builder.Append("END;");
        yield return builder.ToString();
    }

    private static string RenderCreateIndex(RelationalIndex index)
    {
        var table = new RelationalTable(index.Table, index.Schema, [], null);
        var unique = index.IsUnique ? "UNIQUE " : "";
        return $"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N{QuoteStringLiteral(index.Name)} AND object_id = OBJECT_ID(N{QuoteStringLiteral(QualifiedName(table))})) CREATE {unique}INDEX {Quote(index.Name)} ON {QualifiedName(table)} ({RenderColumnList(index.Columns)});";
    }

    private static string RenderColumn(RelationalColumn column)
    {
        var nullability = column.IsNullable ? "NULL" : "NOT NULL";
        return $"{Quote(column.Name)} {column.StoreType} {nullability}";
    }

    private static string RenderColumnList(IEnumerable<string> columns)
    {
        return string.Join(", ", columns.Select(Quote));
    }

    private static string QualifiedName(RelationalTable table)
    {
        return string.IsNullOrWhiteSpace(table.Schema)
            ? Quote(table.Name)
            : $"{Quote(table.Schema)}.{Quote(table.Name)}";
    }

    private static string Quote(string identifier)
    {
        return $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";
    }

    private static string QuoteStringLiteral(string value)
    {
        return $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";
    }
}
