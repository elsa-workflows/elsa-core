using Elsa.Persistence.VNext.Relational.Documents;

namespace Elsa.Persistence.VNext.Sqlite;

public class SqliteDocumentStoreDialect : RelationalDocumentStoreDialect
{
    public override string Parameter(string name)
    {
        return $"${name}";
    }

    public override string QuoteIdentifier(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
