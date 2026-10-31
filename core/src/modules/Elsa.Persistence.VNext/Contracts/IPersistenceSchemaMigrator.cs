namespace Elsa.Persistence.VNext.Contracts;

public interface IPersistenceSchemaMigrator<in TContext>
{
    Task<PersistenceSchemaMigrationResult> MigrateAsync(TContext context, PersistenceSchema schema, CancellationToken cancellationToken = default);
}
