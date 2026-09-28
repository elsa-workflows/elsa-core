using Elsa.Common.Entities;
using Elsa.Persistence.Dapper.Abstractions;
using Elsa.Persistence.Dapper.Contracts;

namespace Elsa.Persistence.Dapper.Dialects;

/// <summary>
/// PostgreSQL dialect that emits quoted identifiers to match FluentMigrator's
/// <c>ForceQuote=true</c> table and column names.
/// </summary>
/// <remarks>
/// FluentMigrator 7.2's PostgreSQL processor quotes identifiers, so a table created as
/// <c>ActivityExecutionRecords</c> is stored as <c>"ActivityExecutionRecords"</c>.
/// Unquoted SQL folds to lowercase (<c>activityexecutionrecords</c>) and misses those objects
/// (42P01). This dialect re-implements <see cref="ISqlDialect"/> so calls through
/// <see cref="ISqlDialect"/> (the Dapper query builder) are quoted without changing
/// <see cref="SqlDialectBase"/> or other providers.
///
/// <para>
/// Compatibility: a handmade lowercase, unquoted schema (<c>create table activityexecutionrecords</c>)
/// will <em>not</em> match quoted <c>"ActivityExecutionRecords"</c>. The supported PG path is the
/// FluentMigrator-created mixed-case schema. Handmade lowercase schemas must be renamed to the
/// quoted mixed-case identifiers (or recreated by the migrations) before using this dialect.
/// </para>
/// </remarks>
public class PostgreSqlDialect : SqlDialectBase, ISqlDialect
{
    /// <inheritdoc />
    public override string From(string table) => From(table, "*");

    /// <inheritdoc />
    public override string From(string table, params string[] fields)
    {
        var fieldList = string.Join(", ", fields.Select(QuoteField));
        return $"select {fieldList} from {Quote(table)} where 1=1";
    }

    /// <inheritdoc />
    public override string And(string field) => $"and {Quote(field)} = @{field}";

    /// <inheritdoc />
    public override string AndNot(string field) => $"and not {Quote(field)} = @{field}";

    /// <inheritdoc />
    public override string And(string field, string[] fieldParamNames) => $"and {Quote(field)} in ({string.Join(", ", fieldParamNames)})";

    /// <inheritdoc />
    public override string AndNot(string field, string[] fieldParamNames) => $"and {Quote(field)} not in ({string.Join(", ", fieldParamNames)})";

    /// <inheritdoc />
    public override string OrderBy(string field, OrderDirection direction)
    {
        var directionString = direction == OrderDirection.Ascending ? "asc" : "desc";
        return $"order by {Quote(field)} {directionString}";
    }

    /// <inheritdoc />
    public override string Upsert(string table, string primaryKeyField, string[] fields, Func<string, string>? getParamName = null)
    {
        getParamName ??= static x => x;
        var fieldList = string.Join(", ", fields.Select(Quote));
        var fieldParamList = string.Join(", ", fields.Select(x => $"@{getParamName(x)}"));
        var updateList = string.Join(", ", fields.Select(x => $"{Quote(x)} = @{getParamName(x)}"));
        // Include the PK in the insert list: ON CONFLICT cannot insert a NOT NULL PK that was omitted (23502).
        return $"insert into {Quote(table)} ({Quote(primaryKeyField)}, {fieldList}) values (@{getParamName(primaryKeyField)}, {fieldParamList}) on conflict({Quote(primaryKeyField)}) do update set {updateList}";
    }

    string ISqlDialect.Delete(string table) => $"delete from {Quote(table)} where 1=1";

    string ISqlDialect.Count(string table) => ((ISqlDialect)this).Count("*", table);

    string ISqlDialect.Count(string fieldExpression, string table) =>
        $"select COUNT({QuoteFieldExpression(fieldExpression)}) from {Quote(table)} where 1=1";

    string ISqlDialect.IsNull(string field) => $"and {Quote(field)} is null";

    string ISqlDialect.IsNotNull(string field) => $"and {Quote(field)} is not null";

    string ISqlDialect.Insert(string table, string[] fields, Func<string, string>? getParamName)
    {
        getParamName ??= static x => x;
        var fieldList = string.Join(", ", fields.Select(Quote));
        var fieldParamList = string.Join(", ", fields.Select(x => $"@{getParamName(x)}"));
        return $"INSERT INTO {Quote(table)} ({fieldList}) VALUES ({fieldParamList});";
    }

    string ISqlDialect.Update(string table, string primaryKeyField, string[] fields, Func<string, string>? getParamName)
    {
        getParamName ??= static x => x;
        var fieldList = string.Join(", ", fields.Select(x => $"{Quote(x)} = @{getParamName(x)}"));
        return $"UPDATE {Quote(table)} SET {fieldList} WHERE {Quote(primaryKeyField)} = @{getParamName(primaryKeyField)};";
    }

    string ISqlDialect.Update(string table, string[] fields, Func<string, string>? getParamName)
    {
        getParamName ??= static x => x;
        var fieldList = string.Join(", ", fields.Select(x => $"{Quote(x)} = @{getParamName(x)}"));
        return $"UPDATE {Quote(table)} SET {fieldList} WHERE 1=1";
    }

    /// <summary>
    /// Quotes a PostgreSQL identifier. <c>*</c> is left as-is. Already-quoted names are returned unchanged.
    /// </summary>
    internal static string Quote(string identifier)
    {
        if (string.IsNullOrEmpty(identifier) || identifier == "*")
            return identifier;

        if (identifier.Length >= 2 && identifier[0] == '"' && identifier[^1] == '"')
            return identifier;

        return string.Concat("\"", identifier.Replace("\"", "\"\""), "\"");
    }

    private static string QuoteField(string field) => field == "*" ? "*" : Quote(field);

    private static string QuoteFieldExpression(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression) || expression == "*")
            return expression;

        var parts = expression.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
            return Quote(parts[0]);

        parts[^1] = Quote(parts[^1]);
        return string.Join(' ', parts);
    }
}
