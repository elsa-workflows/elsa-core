using Elsa.Dapper.Abstractions;

namespace Elsa.Dapper.Dialects;

/// <summary>
/// Represents a SQL Server dialect.
/// </summary>
public class PostgreSqlDialect : SqlDialectBase
{
    /// <inheritdoc />
    public override string Upsert(string table, string primaryKeyField, string[] fields, Func<string, string>? getParamName = default)
    {
        getParamName ??= x => x;
        var fieldList = string.Join(", ", fields);
        var fieldParamNames = fields.Select(x => $"@{getParamName(x)}");
        var fieldParamList = string.Join(", ", fieldParamNames);
        var updateList = string.Join(", ", fields.Select(x => $"{x} = @{getParamName(x)}"));
        return $"insert into {table} ({fieldList}) values ({fieldParamList}) on conflict({primaryKeyField}) do update set {updateList}";
    }
}