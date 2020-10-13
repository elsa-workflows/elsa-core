using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Extensions
{
    public static class WorkflowRegistryExtensions
    {
        public static Task<IWorkflowBlueprint?> GetWorkflowAsync<T>(
            this IWorkflowRegistry workflowRegistry,
            CancellationToken cancellationToken) =>
            workflowRegistry.GetWorkflowAsync(typeof(T).Name, VersionOptions.Latest, cancellationToken);

        public static async Task<IEnumerable<(IWorkflowBlueprint Workflow, IActivityBlueprint Activity)>>
            GetWorkflowsByStartActivityAsync<T>(
                this IWorkflowRegistry workflowRegistry,
                CancellationToken cancellationToken = default)
            where T : IActivity
        {
            var results = await workflowRegistry.GetWorkflowsByStartActivityAsync(typeof(T).Name, cancellationToken);
            return results.Select(x => (x.Workflow, x.Activity));
        }

        public static async Task<IEnumerable<(IWorkflowBlueprint Workflow, IActivityBlueprint Activity)>>
            GetWorkflowsByStartActivityAsync(
                this IWorkflowRegistry workflowRegistry,
                string activityType,
                CancellationToken cancellationToken = default)
        {
            var workflows = await workflowRegistry.GetWorkflowsAsync(cancellationToken);

            var query =
                from workflow in workflows
                where workflow.IsPublished
                from activity in workflow.GetStartActivities()
                where activity.Type == activityType
                select (workflow, activity);

            return query.Distinct();
        }
    }
}