using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public class WorkflowHost : IWorkflowHost
    {
        private readonly IScheduler scheduler;
        private readonly IWorkflowRegistry workflowRegistry;

        public WorkflowHost(
            IScheduler scheduler,
            IWorkflowRegistry workflowRegistry)
        {
            this.scheduler = scheduler;
            this.workflowRegistry = workflowRegistry;
        }
        
        public async Task<WorkflowExecutionContext> RunAsync(string id, object? input = default, CancellationToken cancellationToken = default)
        {
            var workflow = await workflowRegistry.GetWorkflowAsync(id, VersionOptions.Published, cancellationToken);
            return await scheduler.ScheduleActivityAsync(workflow, input, cancellationToken);
        }
    }
}