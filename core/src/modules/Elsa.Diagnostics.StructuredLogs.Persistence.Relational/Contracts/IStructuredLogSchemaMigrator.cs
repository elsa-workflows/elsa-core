namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Contracts;

public interface IStructuredLogSchemaMigrator
{
    ValueTask MigrateAsync(CancellationToken cancellationToken = default);
}
