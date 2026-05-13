namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Contracts;

public interface IRelationalStructuredLogDialect
{
    string ProviderName { get; }

    string ParameterPrefix { get; }

    string QuoteIdentifier(string identifier);

    string ApplyLimit(string sql, int limit);

    string ApplyOffset(string sql, int offset);
}
