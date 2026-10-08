namespace Elsa.Workflows.Management.Entities;

/// <summary>
/// Stores the latest workflow-definition registry generation for a tenant.
/// </summary>
public class WorkflowDefinitionRegistryGeneration
{
    /// <summary>
    /// The tenant ID, or <see cref="Elsa.Common.Multitenancy.Tenant.AgnosticTenantId"/> for tenant-agnostic definitions.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// The latest generation observed for the tenant.
    /// </summary>
    public long Generation { get; set; }
}
