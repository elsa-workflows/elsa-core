using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Documents;
using Elsa.ModularPersistence.Relational.Contracts;

namespace Elsa.ModularPersistence.PostgreSql.Services;

/// <summary>
/// PostgreSQL implementation of the portable document store.
/// </summary>
public sealed class PostgreSqlDocumentStore(IRelationalConnectionFactory connectionFactory, StorageManifestDescriptor manifest) : IDocumentStore
{
    public ValueTask<IDocumentSession> OpenSessionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<IDocumentSession>(new PostgreSqlDocumentSession(connectionFactory, manifest));
    }
}
