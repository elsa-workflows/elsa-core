using System.Text;
using Elsa.Common.Models;
using Elsa.Dapper.Abstractions;

namespace Elsa.Dapper.Dialects;

/// <summary>
/// Represents a SQLite dialect.
/// </summary>
public class SqliteDialect : SqlDialectBase
{
    /// <inheritdoc />
    public override string Skip(int count) => $"offset {count}";

    /// <inheritdoc />
    public override string Take(int count) => $"limit {count}";

    /// <inheritdoc />
    public override string Page(PageArgs pageArgs)
    {
        var sb = new StringBuilder();

        // Attention: the order is important here for SQLite (LIMIT must come before OFFSET).
        if (pageArgs.Limit != null)
            sb.AppendLine(Take(pageArgs.Limit.Value));

        if (pageArgs.Offset != null)
            sb.AppendLine(Skip(pageArgs.Offset.Value));

        return sb.ToString();
    }
    
    public override string Upsert(string table, string primaryKeyField, string[] fields, Func<string, string>? getParamName = default)
    {
        getParamName ??= x => x;
        var fieldList = string.Join(", ", fields);
        var fieldParamNames = fields.Select(x => $"@{getParamName(x)}");
        var fieldParamList = string.Join(", ", fieldParamNames);
        return $"INSERT OR REPLACE INTO {table} ({primaryKeyField}, {fieldList}) VALUES (@{getParamName(primaryKeyField)}, {fieldParamList});";
    }
}