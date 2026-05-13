using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Contracts;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.Services;

public class SqliteStructuredLogDialect : IRelationalStructuredLogDialect
{
    public string ProviderName => "SQLite";
    public string ParameterPrefix => "@";

    public string QuoteIdentifier(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    public string ApplyLimit(string sql, int limit)
    {
        return $"{sql} LIMIT {limit}";
    }
}
