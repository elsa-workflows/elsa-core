using System.Data;
using System.Data.Common;
using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Contracts;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Models;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Services;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Stores;

public class RelationalStructuredLogStore(
    IRelationalStructuredLogConnectionFactory connectionFactory,
    IRelationalStructuredLogDialect dialect,
    RelationalStructuredLogSqlBuilder sqlBuilder,
    RelationalStructuredLogMapper mapper) : IStructuredLogStore
{
    public async ValueTask WriteAsync(StructuredLogEvent logEvent, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await InsertAsync(connection, mapper.Map(logEvent), null, cancellationToken);
    }

    public async ValueTask WriteManyAsync(IReadOnlyCollection<StructuredLogEvent> logEvents, CancellationToken cancellationToken = default)
    {
        if (logEvents.Count == 0)
            return;

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        foreach (var logEvent in logEvents)
            await InsertAsync(connection, mapper.Map(logEvent), transaction, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async ValueTask<RecentStructuredLogsResult> QueryAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default)
    {
        var query = sqlBuilder.BuildQuery(filter);
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = CreateCommand(connection, query.Sql, query.Parameters);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<StructuredLogEvent>();

        while (await reader.ReadAsync(cancellationToken))
            items.Add(mapper.Map(reader));

        items.Reverse();
        return new(items, 0);
    }

    public async ValueTask<IReadOnlyCollection<StructuredLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = CreateCommand(connection, sqlBuilder.BuildListSources());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var sources = new List<StructuredLogSource>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var sourceId = reader.GetString(reader.GetOrdinal("SourceId"));
            var lastSeen = RelationalStructuredLogMapper.ParseTimestamp(reader.GetString(reader.GetOrdinal("LastSeen")));
            sources.Add(new()
            {
                Id = sourceId,
                DisplayName = sourceId,
                MachineName = sourceId,
                ProcessId = 0,
                LastSeen = lastSeen,
                Status = StructuredLogSourceStatus.Connected
            });
        }

        return sources;
    }

    private async ValueTask InsertAsync(DbConnection connection, RelationalStructuredLogRecord record, DbTransaction? transaction, CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(connection, sqlBuilder.BuildInsert(), CreateParameters(record));
        command.Transaction = transaction;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private DbCommand CreateCommand(DbConnection connection, string sql, IReadOnlyDictionary<string, object?>? parameters = null)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandType = CommandType.Text;

        if (parameters == null)
            return command;

        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name.StartsWith(dialect.ParameterPrefix, StringComparison.Ordinal) ? name : $"{dialect.ParameterPrefix}{name}";
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        return command;
    }

    private static IReadOnlyDictionary<string, object?> CreateParameters(RelationalStructuredLogRecord record)
    {
        return new Dictionary<string, object?>
        {
            ["Id"] = record.Id,
            ["Sequence"] = record.Sequence,
            ["Timestamp"] = record.Timestamp,
            ["ReceivedAt"] = record.ReceivedAt,
            ["Level"] = (int)record.Level,
            ["Category"] = record.Category,
            ["EventId"] = record.EventId,
            ["EventName"] = record.EventName,
            ["Message"] = record.Message,
            ["MessageTemplate"] = record.MessageTemplate,
            ["ExceptionJson"] = record.ExceptionJson,
            ["ScopesJson"] = record.ScopesJson,
            ["PropertiesJson"] = record.PropertiesJson,
            ["TraceId"] = record.TraceId,
            ["SpanId"] = record.SpanId,
            ["CorrelationId"] = record.CorrelationId,
            ["TenantId"] = record.TenantId,
            ["WorkflowDefinitionId"] = record.WorkflowDefinitionId,
            ["WorkflowInstanceId"] = record.WorkflowInstanceId,
            ["SourceId"] = record.SourceId
        };
    }
}
