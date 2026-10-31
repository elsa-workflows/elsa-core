using System.Data.Common;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Contracts;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Services;

public class StructuredLogRetentionService(
    IRelationalStructuredLogConnectionFactory connectionFactory,
    IRelationalStructuredLogDialect dialect,
    RelationalStructuredLogSqlBuilder sqlBuilder,
    IOptions<RelationalStructuredLogOptions> options)
{
    public async ValueTask CleanupAsync(CancellationToken cancellationToken = default)
    {
        var retention = options.Value.Retention;

        if (retention.MaxAge == null && retention.MaxRows == null)
            return;

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        if (retention.MaxAge is { } maxAge)
        {
            var cutoff = RelationalStructuredLogMapper.FormatTimestamp(DateTimeOffset.UtcNow.Subtract(maxAge));
            await ExecuteAsync(connection, sqlBuilder.BuildDeleteOlderThan(cutoff), cancellationToken);
        }

        if (retention.MaxRows is { } maxRows and >= 0)
            await ExecuteAsync(connection, sqlBuilder.BuildDeleteRowsBeyondMax(maxRows), cancellationToken);
    }

    private async ValueTask ExecuteAsync(DbConnection connection, QueryDefinition query, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = query.Sql;

        foreach (var (name, value) in query.Parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name.StartsWith(dialect.ParameterPrefix, StringComparison.Ordinal) ? name : $"{dialect.ParameterPrefix}{name}";
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
