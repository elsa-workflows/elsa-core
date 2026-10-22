namespace Elsa.Workflows.Management.Entities;

/// <summary>
/// Stores the latest workflow-definition registry generation for a tenant.
/// </summary>
public class WorkflowDefinitionRegistryGeneration
{
    /// <summary>
    /// The tenant ID, or <see cref="Elsa.Common.Multitenancy.Tenant.AgnosticTenantId"/> for tenant-agnostic definitions.
    /// </summary>
    /// <remarks>
    /// On Oracle the default tenant is stored as NULL. EF Core does not run the empty-string converter
    /// for database NULL, so this property materializes as <c>null</c>. Treat <c>null</c> as
    /// <see cref="Elsa.Common.Multitenancy.Tenant.DefaultTenantId"/>.
    /// </remarks>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// The latest generation observed for the tenant.
    /// </summary>
    public long Generation { get; set; }
}
