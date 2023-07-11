using System.Threading;
using System.Threading.Tasks;
using Elsa.Server.Orleans.Grains.Contracts;
using Elsa.Services;
using Orleans;
using Orleans.Concurrency;

namespace Elsa.Server.Orleans.Grains
{
    [StatelessWorker]
    public class WorkflowDefinitionGrain : Grain, IWorkflowDefinitionGrain
    {
        private readonly IWorkflowLaunchpad _workflowLaunchpad;
        public WorkflowDefinitionGrain(IWorkflowLaunchpad workflowLaunchpad) => _workflowLaunchpad = workflowLaunchpad;

        public async Task ExecuteWorkflowAsync(ExecuteWorkflowDefinitionRequest request, CancellationToken cancellationToken = default) =>
            await _workflowLaunchpad.FindAndExecuteStartableWorkflowAsync(request.WorkflowDefinitionId, request.ActivityId, request.CorrelationId, request.ContextId, request.Input, request.TenantId, request.IgnoreAlreadyRunningAndSingleton, cancellationToken);
    }
}