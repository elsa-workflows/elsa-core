using System.Data.Common;

namespace Elsa.ModularPersistence.Relational.Contracts;

/// <summary>
/// Opens provider-specific relational connections.
/// </summary>
public interface IRelationalConnectionFactory
{
    ValueTask<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}
