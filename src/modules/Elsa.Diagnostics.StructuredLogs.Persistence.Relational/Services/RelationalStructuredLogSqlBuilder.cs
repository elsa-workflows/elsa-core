using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Contracts;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Services;

public class RelationalStructuredLogSqlBuilder(IRelationalStructuredLogDialect dialect)
{
    private readonly string _table = dialect.QuoteIdentifier("StructuredLogEvents");

    public string BuildInsert()
    {
        var columns = Columns;
        var columnList = string.Join(", ", columns.Select(dialect.QuoteIdentifier));
        var parameterList = string.Join(", ", columns.Select(x => $"{dialect.ParameterPrefix}{x}"));
        return $"INSERT INTO {_table} ({columnList}) VALUES ({parameterList})";
    }

    public QueryDefinition BuildQuery(StructuredLogFilter filter)
    {
        var parameters = new Dictionary<string, object?>();
        var predicates = BuildFilterPredicates(filter, parameters);
        var where = predicates.Count == 0 ? "" : $" WHERE {string.Join(" AND ", predicates)}";
        var limit = Math.Clamp(filter.Take ?? 100, 0, 1000);
        var sql = $"SELECT {string.Join(", ", Columns.Select(dialect.QuoteIdentifier))} FROM {_table}{where} ORDER BY {dialect.QuoteIdentifier("Timestamp")} DESC, {dialect.QuoteIdentifier("ReceivedAt")} DESC, {dialect.QuoteIdentifier("Sequence")} DESC, {dialect.QuoteIdentifier("Id")} DESC";
        sql = dialect.ApplyLimit(sql, limit);
        return new(sql, parameters);
    }

    public string BuildListSources()
    {
        var sourceId = dialect.QuoteIdentifier("SourceId");
        var receivedAt = dialect.QuoteIdentifier("ReceivedAt");
        return $"SELECT {sourceId}, MAX({receivedAt}) AS {dialect.QuoteIdentifier("LastSeen")} FROM {_table} GROUP BY {sourceId} ORDER BY {sourceId}";
    }

    public QueryDefinition BuildDeleteOlderThan(string cutoff)
    {
        return new($"DELETE FROM {_table} WHERE {dialect.QuoteIdentifier("ReceivedAt")} < {dialect.ParameterPrefix}Cutoff", new Dictionary<string, object?> { ["Cutoff"] = cutoff });
    }

    public QueryDefinition BuildDeleteRowsBeyondMax(int maxRows)
    {
        var id = dialect.QuoteIdentifier("Id");
        var receivedAt = dialect.QuoteIdentifier("ReceivedAt");
        var sequence = dialect.QuoteIdentifier("Sequence");
        var sql = $"DELETE FROM {_table} WHERE {id} IN (SELECT {id} FROM {_table} ORDER BY {receivedAt} DESC, {sequence} DESC, {id} DESC LIMIT -1 OFFSET {dialect.ParameterPrefix}MaxRows)";
        return new(sql, new Dictionary<string, object?> { ["MaxRows"] = maxRows });
    }

    private List<string> BuildFilterPredicates(StructuredLogFilter filter, IDictionary<string, object?> parameters)
    {
        var predicates = new List<string>();

        if (filter.MinimumLevel is { } minimumLevel)
            AddPredicate(predicates, parameters, "Level", ">=", (int)minimumLevel);

        if (filter.Levels is { Count: > 0 })
        {
            var names = filter.Levels.Select((level, index) =>
            {
                var name = $"Level{index}";
                parameters[name] = (int)level;
                return $"{dialect.ParameterPrefix}{name}";
            });
            predicates.Add($"{dialect.QuoteIdentifier("Level")} IN ({string.Join(", ", names)})");
        }

        if (!string.IsNullOrWhiteSpace(filter.CategoryPrefix))
            AddPredicate(predicates, parameters, "Category", "LIKE", $"{filter.CategoryPrefix}%");

        if (!string.IsNullOrWhiteSpace(filter.Text))
        {
            parameters["Text"] = $"%{filter.Text}%";
            var textParameter = $"{dialect.ParameterPrefix}Text";
            predicates.Add($"({dialect.QuoteIdentifier("Message")} LIKE {textParameter} OR {dialect.QuoteIdentifier("MessageTemplate")} LIKE {textParameter} OR {dialect.QuoteIdentifier("Category")} LIKE {textParameter} OR {dialect.QuoteIdentifier("ExceptionJson")} LIKE {textParameter} OR {dialect.QuoteIdentifier("ScopesJson")} LIKE {textParameter} OR {dialect.QuoteIdentifier("PropertiesJson")} LIKE {textParameter})");
        }

        AddStringPredicate(predicates, parameters, "TenantId", filter.TenantId);
        AddStringPredicate(predicates, parameters, "WorkflowDefinitionId", filter.WorkflowDefinitionId);
        AddStringPredicate(predicates, parameters, "WorkflowInstanceId", filter.WorkflowInstanceId);
        AddStringPredicate(predicates, parameters, "TraceId", filter.TraceId);
        AddStringPredicate(predicates, parameters, "SpanId", filter.SpanId);
        AddStringPredicate(predicates, parameters, "CorrelationId", filter.CorrelationId);
        AddStringPredicate(predicates, parameters, "SourceId", filter.SourceId);

        if (filter.From is { } from)
            AddPredicate(predicates, parameters, "Timestamp", ">=", RelationalStructuredLogMapper.FormatTimestamp(from));

        if (filter.To is { } to)
            AddPredicate(predicates, parameters, "Timestamp", "<=", RelationalStructuredLogMapper.FormatTimestamp(to));

        return predicates;
    }

    private void AddStringPredicate(ICollection<string> predicates, IDictionary<string, object?> parameters, string column, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        AddPredicate(predicates, parameters, column, "=", value);
    }

    private void AddPredicate(ICollection<string> predicates, IDictionary<string, object?> parameters, string column, string op, object value)
    {
        predicates.Add($"{dialect.QuoteIdentifier(column)} {op} {dialect.ParameterPrefix}{column}");
        parameters[column] = value;
    }

    private static readonly string[] Columns =
    [
        "Id",
        "Sequence",
        "Timestamp",
        "ReceivedAt",
        "Level",
        "Category",
        "EventId",
        "EventName",
        "Message",
        "MessageTemplate",
        "ExceptionJson",
        "ScopesJson",
        "PropertiesJson",
        "TraceId",
        "SpanId",
        "CorrelationId",
        "TenantId",
        "WorkflowDefinitionId",
        "WorkflowInstanceId",
        "SourceId"
    ];
}

public record QueryDefinition(string Sql, IReadOnlyDictionary<string, object?> Parameters);
