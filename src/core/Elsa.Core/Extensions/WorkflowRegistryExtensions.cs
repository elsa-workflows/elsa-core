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
        public static Task<Workflow> GetWorkflowAsync<T>(this IWorkflowRegistry workflowRegistry, CancellationToken cancellationToken) =>
            workflowRegistry.GetWorkflowAsync(typeof(T).Name, VersionOptions.Latest, cancellationToken);

        public static Task<IEnumerable<(Workflow Workflow, IActivity Activity)>> GetWorkflowsByStartActivityAsync<T>(
            this IWorkflowRegistry workflowRegistry,
            CancellationToken cancellationToken = default)
            where T : IActivity =>
            workflowRegistry.GetWorkflowsByStartActivityAsync(typeof(T).Name, cancellationToken);

        public static async Task<IEnumerable<(Workflow Workflow, IActivity Activity)>> GetWorkflowsByStartActivityAsync(
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