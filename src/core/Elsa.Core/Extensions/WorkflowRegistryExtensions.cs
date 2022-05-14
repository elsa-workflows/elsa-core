using System.Threading;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa
{
    public static class WorkflowRegistryExtensions
    {
        public static Task<IWorkflowBlueprint?> GetWorkflowAsync<T>(
            this IWorkflowRegistry workflowRegistry,
            string? tenantId,
            CancellationToken cancellationToken = default) where T : IWorkflow =>
            workflowRegistry.FindAsync(typeof(T).Name, VersionOptions.Latest, tenantId, cancellationToken);

        public static Task<IWorkflowBlueprint?> GetWorkflowAsync<T>(
            this IWorkflowRegistry workflowRegistry,
            CancellationToken cancellationToken = default) where T : IWorkflow =>
            workflowRegistry.GetWorkflowAsync<T>(default, cancellationToken);

        public static Task<IWorkflowBlueprint?> GetWorkflowAsync(
            this IWorkflowRegistry workflowRegistry,
            string id,
            VersionOptions versionOptions,
            CancellationToken cancellationToken = default) =>
            workflowRegistry.GetWorkflowAsync(id, default, versionOptions, cancellationToken);

        public static Task<IWorkflowBlueprint?> GetWorkflowAsync(
            this IWorkflowRegistry workflowRegistry,
            string id,
            string? tenantId,
            VersionOptions versionOptions,
            CancellationToken cancellationToken = default) =>
            workflowRegistry.FindAsync(id, versionOptions, tenantId, cancellationToken);
    }
}