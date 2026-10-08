using Elsa.Persistence.VNext.Relational.Documents;

namespace Elsa.Persistence.VNext.PostgreSql;

public class PostgreSqlDocumentStoreDialect : RelationalDocumentStoreDialect
{
    public override string Parameter(string name)
    {
        return $"@{name}";
    }

    public override string QuoteIdentifier(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    public override IReadOnlyList<string> CreateMaterializationLockStatements()
    {
        return [
            "SELECT pg_advisory_xact_lock(hashtext('Elsa.Persistence.VNext.Documents'));"
        ];
    }
}
