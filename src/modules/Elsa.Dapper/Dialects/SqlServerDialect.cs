using Elsa.Dapper.Abstractions;

namespace Elsa.Dapper.Dialects;

/// <summary>
/// Represents a SQL Server dialect.
/// </summary>
public class SqlServerDialect : SqlDialectBase
{
    /// <inheritdoc />
    public override string Skip(int count) => $"OFFSET {count} ROWS";

    /// <inheritdoc />
    public override string Take(int count) => $"FETCH NEXT {count} ROWS ONLY";
}