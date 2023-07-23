using Elsa.Common.Entities;
using Elsa.Dapper.Contracts;

namespace Elsa.Dapper.Abstractions;

/// <summary>
/// Provides a base implementation of <see cref="ISqlDialect"/>, where the dialect defaults to SQLite.
/// </summary>
public abstract class SqlDialectBase : ISqlDialect
{
    /// <inheritdoc />
    public virtual string From(string table) => From(table, "*");

    /// <inheritdoc />
    public virtual string From(string table, params string[] fields)
    {
        var fieldList = string.Join(", ", fields);
        return $"select {fieldList} from {table} where 1=1";
    }

    /// <inheritdoc />
    public string Delete(string table) => $"delete from {table} where 1=1";

    /// <inheritdoc />
    public string Count(string table) => Count("*", table);

    /// <inheritdoc />
    public string Count(string fieldExpression, string table) => $"select COUNT({fieldExpression}) from {table} where 1=1";

    /// <inheritdoc />
    public virtual string And(string field) => $"and {field} = @{field}";

    /// <inheritdoc />
    public virtual string AndNot(string field) => $"and not {field} = @{field}";

    /// <inheritdoc />
    public virtual string And(string field, string[] fieldParamNames) => $"and {field} in ({string.Join(", ", fieldParamNames)})";

    /// <inheritdoc />
    public virtual string AndNot(string field, string[] fieldParamNames) => $"and {field} not in ({string.Join(", ", fieldParamNames)})";

    /// <inheritdoc />
    public string IsNull(string field) => $"and {field} is null";

    /// <inheritdoc />
    public string IsNotNull(string field) => $"and {field} is not null";

    /// <inheritdoc />
    public virtual string OrderBy(string field, OrderDirection direction)
    {
        var directionString = direction == OrderDirection.Ascending ? "asc" : "desc";
        return $"order by {field} {directionString}";
    }

    /// <inheritdoc />
    public virtual string Skip(int count) => $"OFFSET {count}";

    /// <inheritdoc />
    public virtual string Take(int count) => $"LIMIT {count}";

    /// <inheritdoc />
    public string Insert(string table, string[] fields)
    {
        var fieldList = string.Join(", ", fields);
        var fieldParamNames = fields.Select(x => $"@{x}");
        var fieldParamList = string.Join(", ", fieldParamNames);
        return $"INSERT INTO {table} ({fieldList}) VALUES ({fieldParamList});";
    }

    /// <inheritdoc />
    public virtual string Upsert(string table, string primaryKeyField, string[] fields)
    {
        var fieldList = string.Join(", ", fields);
        var fieldParamNames = fields.Select(x => $"@{x}");
        var fieldParamList = string.Join(", ", fieldParamNames);
        //var updateList = string.Join(", ", fields.Select(x => $"{x} = @{x}"));
        //return $"insert into {table} ({fieldList}) values ({fieldParamList}) on conflict(id) do update set {updateList}";
        return $"INSERT OR REPLACE INTO {table} ({primaryKeyField}, {fieldList}) VALUES (@{primaryKeyField}, {fieldParamList});";
    }
}