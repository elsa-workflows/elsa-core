using Elsa.Identity.Models;

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
    /// Deletes the roles matching the specified filter in a single atomic operation and reports whether this call
    /// removed anything.
    /// </summary>
    /// <remarks>
    /// Implementations must decide the outcome atomically, so that exactly one of two concurrent calls matching the
    /// same role observes <see langword="true"/>. Loading the roles and then deleting them in a separate step does
    /// not satisfy this contract.
    /// </remarks>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when this call removed at least one role; otherwise, <see langword="false"/>.</returns>
    Task<bool> TryDeleteAsync(RoleFilter filter, CancellationToken cancellationToken = default);
}
