using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Documents;
using Elsa.ModularPersistence.Relational.Contracts;

namespace Elsa.ModularPersistence.SqlServer.Services;

/// <summary>
/// SQL Server implementation of the portable document store.
/// </summary>
public sealed class SqlServerDocumentStore(IRelationalConnectionFactory connectionFactory, StorageManifestDescriptor manifest) : IDocumentStore
{
    public ValueTask<IDocumentSession> OpenSessionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<IDocumentSession>(new SqlServerDocumentSession(connectionFactory, manifest));
    }
}
