using Elsa.Extensions;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Parameters;
using Elsa.Workflows.Runtime.Results;
using Elsa.Workflows.State;
using Proto.Cluster;

namespace Elsa.ProtoActor.Services;

public class ProtoActorWorkflowClient(Cluster cluster) : IWorkflowClient
{
    /// <inheritdoc />
    public string WorkflowInstanceId { get; set; }

    /// <inheritdoc />
    public async Task<ExecuteWorkflowResult> ExecuteAndWaitAsync(IExecuteWorkflowParams? @params = null, CancellationToken cancellationToken = default)
    {
        var grain = cluster.GetNamedWorkflowGrain(WorkflowInstanceId);
        //var response = await client.Start(request, @params?.CancellationToken ?? default);

        throw new NotImplementedException();
    }

    public async Task ExecuteAndForgetAsync(IExecuteWorkflowParams? @params = default, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<CancellationResult> CancelAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<WorkflowState> ExportStateAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task ImportStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}