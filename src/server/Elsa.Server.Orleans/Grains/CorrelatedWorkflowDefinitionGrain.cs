using System.Threading;
using System.Threading.Tasks;
using Elsa.Server.Orleans.Grains.Contracts;
using Elsa.Services;
using Elsa.Services.Models;
using Orleans;
using Orleans.Concurrency;

namespace Elsa.Server.Orleans.Grains
{
    [StatelessWorker]
    public class CorrelatedWorkflowDefinitionGrain : Grain, ICorrelatedWorkflowGrain
    {
        private readonly IWorkflowLaunchpad _workflowLaunchpad;
        public CorrelatedWorkflowDefinitionGrain(IWorkflowLaunchpad workflowLaunchpad) => _workflowLaunchpad = workflowLaunchpad;

        public async Task ExecutedCorrelatedWorkflowAsync(TriggerWorkflowsRequest request, CancellationToken cancellationToken = default) => await _workflowLaunchpad.CollectAndExecuteWorkflowsAsync(new WorkflowsQuery(
                request.ActivityType,
                request.Bookmark,
                request.CorrelationId,
                request.WorkflowInstanceId,
                request.ContextId,
                request.TenantId),
            request.Input,
            cancellationToken);
    }
}