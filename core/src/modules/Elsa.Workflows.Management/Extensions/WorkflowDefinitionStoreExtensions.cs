using Elsa.Workflows.Management.Filters;

// ReSharper disable once CheckNamespace
namespace Elsa.Workflows.Management;

internal static class WorkflowDefinitionStoreExtensions
{
    /// <summary>
    /// Version ids are primary keys shared by all tenants. Returns <paramref name="versionId"/> only when no tenant uses it,
    /// so that a definition imported or populated into a second tenant keeps its definition id but gets <paramref name="fallbackId"/>.
    /// </summary>
    internal static async Task<string> ResolveVersionIdAsync(this IWorkflowDefinitionStore store, string? versionId, string fallbackId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(versionId))
            return fallbackId;

        var isTaken = await store.AnyAsync(new WorkflowDefinitionFilter { Id = versionId, TenantAgnostic = true }, cancellationToken);
        return isTaken ? fallbackId : versionId;
    }
}
