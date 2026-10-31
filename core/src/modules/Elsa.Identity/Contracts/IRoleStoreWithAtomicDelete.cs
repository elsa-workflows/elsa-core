namespace Elsa.Identity.Contracts;

/// <summary>
/// An optional capability, implemented alongside <see cref="IRoleStore"/>, that deletes roles atomically and
/// reports whether the calling request is the one that removed them.
/// </summary>
/// <remarks>
/// This capability is deliberately separate from <see cref="IRoleStore"/> so that third-party role stores keep
/// compiling and binding against the unchanged <see cref="IRoleStore.DeleteAsync"/> signature. Callers that need
/// to act only on a deletion they performed themselves, such as one publishing a security notification, probe for
/// this interface and fall back to <see cref="IRoleStore.DeleteAsync"/> when a store does not offer it.
/// </remarks>
public interface IRoleStoreWithAtomicDelete
{
    /// <summary>
    /// Deletes the single role with the given ID within the current tenant scope, atomically, and reports whether
    /// this call removed it.
    /// </summary>
    /// <remarks>
    /// Implementations must decide the outcome atomically, so that exactly one of two concurrent callers for the
    /// same ID observes <see langword="true"/>. Loading the role and then deleting it in a separate step does not
    /// satisfy this contract.
    /// </remarks>
    /// <param name="roleId">The ID of the role to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when this call removed the role; otherwise, <see langword="false"/>.</returns>
    Task<bool> TryDeleteAsync(string roleId, CancellationToken cancellationToken = default);
}
