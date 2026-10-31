using System.Text;
using Elsa.Common.Models;
using Elsa.Persistence.Dapper.Abstractions;
using Elsa.Persistence.Dapper.Contracts;

namespace Elsa.Persistence.Dapper.Dialects;

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
    
    public override string Upsert(string table, string primaryKeyField, string[] fields, Func<string, string>? getParamName = null)
    {
        getParamName ??= x => x;
        var fieldList = string.Join(", ", fields);
        var fieldParamList = string.Join(", ", fields.Select(x => $"@{getParamName(x)}"));
        var updateList = string.Join(", ", fields.Select(x => $"{x} = @{getParamName(x)}"));
        var tenantWhere = SqlDialectStatements.TenantScopedUpdateWhere(fields, static column => column, getParamName);
        // INSERT OR REPLACE deletes the existing row first and cannot be scoped by tenant.
        return $"INSERT INTO {table} ({primaryKeyField}, {fieldList}) VALUES (@{getParamName(primaryKeyField)}, {fieldParamList}) ON CONFLICT({primaryKeyField}) DO UPDATE SET {updateList}{tenantWhere};";
    }
}