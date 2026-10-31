namespace Elsa.Common.Multitenancy;

/// <summary>
/// Raised once after <see cref="ITenantService.ActivateTenantsAsync"/> has activated every tenant, for work that spans all tenants.
/// </summary>
/// <remarks>
/// Not raised for tenants added later by <see cref="ITenantService.RefreshAsync"/>. A handler that throws is logged and
/// the remaining handlers still run; cancellation of the activation is propagated.
/// </remarks>
public interface IAllTenantsActivatedEvent
{
    /// <summary>
    /// Handles the completion of the activation of all tenants.
    /// </summary>
    Task AllTenantsActivatedAsync(CancellationToken cancellationToken);
}
