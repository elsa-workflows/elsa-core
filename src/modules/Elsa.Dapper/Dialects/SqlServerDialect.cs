using Elsa.Dapper.Abstractions;

namespace Elsa.Dapper.Dialects;

/// <summary>
/// Represents a SQL Server dialect.
/// </summary>
public class SqlServerDialect : SqlDialectBase
{
    /// <inheritdoc />
    public override string Skip(int count) => $"Offset {count} Rows";

    /// <inheritdoc />
    public override string Take(int count) => $"fetch next {count} rows only";
}