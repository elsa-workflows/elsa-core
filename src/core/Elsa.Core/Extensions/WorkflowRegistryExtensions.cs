using System.Threading;
using System.Threading.Tasks;
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
            bool isDisabled = false,
            CancellationToken cancellationToken = default) =>
            workflowRegistry.GetAsync(typeof(T).Name, tenantId, VersionOptions.Latest, isDisabled, cancellationToken);

        public static Task<IWorkflowBlueprint?> GetWorkflowAsync<T>(
            this IWorkflowRegistry workflowRegistry,
            bool isDisabled = false,
            CancellationToken cancellationToken = default) =>
            workflowRegistry.GetWorkflowAsync<T>(default, isDisabled, cancellationToken);
        
        public static Task<IWorkflowBlueprint?> GetWorkflowAsync(
            this IWorkflowRegistry workflowRegistry,
            string id,
            VersionOptions versionOptions,
            bool isDisabled = false,
            CancellationToken cancellationToken = default) =>
            workflowRegistry.GetWorkflowAsync(id, default, versionOptions, isDisabled, cancellationToken);
        
        public static Task<IWorkflowBlueprint?> GetWorkflowAsync(
            this IWorkflowRegistry workflowRegistry,
            string id,
            string? tenantId,
            VersionOptions versionOptions,
            bool isDisabled = false,
            CancellationToken cancellationToken = default) =>
            workflowRegistry.GetAsync(id, tenantId, versionOptions, isDisabled, cancellationToken);
    }
}