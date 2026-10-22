using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Re-stamps a CLR workflow definition's <c>TenantId</c> to the tenant declared in code.
/// Used only by <see cref="DefaultWorkflowDefinitionStorePopulator"/>.
/// </summary>
public interface IClrWorkflowDefinitionTenantRestamper
{
    /// <summary>
    /// Finds the CLR row for <paramref name="definitionId"/> and <paramref name="version"/>
    /// without a tenant filter. When its stored tenant differs from
    /// <paramref name="declaredTenantId"/>, updates only that row. Returns the row
    /// (re-stamped or already matching), or <c>null</c> when no CLR row exists.
    /// </summary>
    Task<WorkflowDefinition?> RestampAsync(string definitionId, int version, string declaredTenantId, CancellationToken cancellationToken = default);
}
