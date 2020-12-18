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
            CancellationToken cancellationToken = default) =>
            workflowRegistry.GetWorkflowAsync(typeof(T).Name, tenantId, VersionOptions.Latest, cancellationToken);

        public static Task<IWorkflowBlueprint?> GetWorkflowAsync<T>(
            this IWorkflowRegistry workflowRegistry,
            CancellationToken cancellationToken = default) =>
            workflowRegistry.GetWorkflowAsync<T>(default, cancellationToken);
        
        public static Task<IWorkflowBlueprint?> GetWorkflowAsync(
            this IWorkflowRegistry workflowRegistry,
            string id,
            VersionOptions versionOptions,
            CancellationToken cancellationToken = default) =>
            workflowRegistry.GetWorkflowAsync(id, default, versionOptions, cancellationToken);

        // public static async Task<IEnumerable<(IWorkflowBlueprint Workflow, IActivityBlueprint Activity)>>
        //     GetWorkflowsByStartActivityAsync<T>(
        //         this IWorkflowRegistry workflowRegistry,
        //         CancellationToken cancellationToken = default)
        //     where T : IActivity
        // {
        //     var results = await workflowRegistry.GetWorkflowsByStartActivityAsync(typeof(T).Name, cancellationToken);
        //     return results.Select(x => (x.Workflow, x.Activity));
        // }

        // public static async Task<IEnumerable<(IWorkflowBlueprint Workflow, IActivityBlueprint Activity)>> GetWorkflowsByStartActivityAsync(
        //     this IWorkflowRegistry workflowRegistry,
        //     string activityType,
        //     CancellationToken cancellationToken = default)
        // {
        //     var workflows = await workflowRegistry.GetWorkflowsAsync(cancellationToken).ToListAsync(cancellationToken);
        //
        //     var query =
        //         from workflow in workflows
        //         where workflow.IsPublished
        //         from activity in workflow.GetStartActivities()
        //         where activity.Type == activityType
        //         select (workflow, activity);
        //
        //     return query.Distinct();
        // }
    }
}