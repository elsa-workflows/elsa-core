namespace Elsa.ModularPersistence.Documents;

/// <summary>
/// Opens provider sessions for portable document operations.
/// </summary>
public interface IDocumentStore
{
    ValueTask<IDocumentSession> OpenSessionAsync(CancellationToken cancellationToken = default);
}
