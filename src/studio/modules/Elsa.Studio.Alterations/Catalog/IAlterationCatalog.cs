namespace Elsa.Studio.Alterations.Catalog;

/// <summary>
/// Source of available alteration descriptors. Currently a static built-in list; designed so
/// it can be swapped for a server-driven implementation once the API exposes a discovery endpoint.
/// </summary>
public interface IAlterationCatalog
{
    /// <summary>Returns all known alterations (server-side type id, display info, config fields).</summary>
    ValueTask<IReadOnlyList<AlterationDescriptor>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Looks up a single descriptor by its server-side type id.</summary>
    ValueTask<AlterationDescriptor?> FindAsync(string typeId, CancellationToken cancellationToken = default);
}
