using System.Threading;
using System.Threading.Tasks;
using Elsa.Server.Orleans.Grains.Contracts;
using Elsa.Services;
using Orleans;

namespace Elsa.Server.Orleans.Dispatch
{
    public class GrainWorkflowDispatcher : IWorkflowDefinitionDispatcher, IWorkflowInstanceDispatcher, IWorkflowDispatcher
    {
        private readonly IClusterClient  _clusterClient;

        public GrainWorkflowDispatcher(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }
        
        public async Task DispatchAsync(ExecuteWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
        {
            var grain = _clusterClient.GetGrain<IWorkflowDefinitionGrain>(request.WorkflowDefinitionId);
            await grain.ExecuteWorkflowAsync(request, cancellationToken);
        }

        public async Task DispatchAsync(ExecuteWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
        {
            var grain = _clusterClient.GetGrain<IWorkflowInstanceGrain>(request.WorkflowInstanceId);
            await grain.ExecuteWorkflowAsync(request, cancellationToken);
        }

        public async Task DispatchAsync(TriggerWorkflowsRequest request, CancellationToken cancellationToken = default)
        {
            var grain = _clusterClient.GetGrain<ICorrelatedWorkflowGrain>(request.CorrelationId);
            await grain.ExecutedCorrelatedWorkflowAsync(request, cancellationToken);
        }
    }
}