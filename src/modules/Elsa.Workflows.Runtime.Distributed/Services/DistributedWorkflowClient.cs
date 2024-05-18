using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Services;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Distributed.Services;

public class DistributedWorkflowClient(string workflowInstanceId, IServiceProvider serviceProvider) : IWorkflowClient
{
    private readonly LocalWorkflowClient _localWorkflowClient = ActivatorUtilities.CreateInstance<LocalWorkflowClient>(serviceProvider, workflowInstanceId);

    public string WorkflowInstanceId => workflowInstanceId;

    public async Task<CreateWorkflowInstanceResponse> CreateInstanceAsync(CreateWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        return await _localWorkflowClient.CreateInstanceAsync(request, cancellationToken);
    }

    public async Task<RunWorkflowInstanceResponse> RunInstanceAsync(RunWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        return await _localWorkflowClient.RunInstanceAsync(request, cancellationToken);
    }

    public async Task<RunWorkflowInstanceResponse> CreateAndRunInstanceAsync(CreateAndRunWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        return await _localWorkflowClient.CreateAndRunInstanceAsync(request, cancellationToken);
    }

    public async Task CancelAsync(CancellationToken cancellationToken = default)
    {
        await _localWorkflowClient.CancelAsync(cancellationToken);
    }

    public async Task<WorkflowState> ExportStateAsync(CancellationToken cancellationToken = default)
    {
        return await _localWorkflowClient.ExportStateAsync(cancellationToken);
    }

    public async Task ImportStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        await _localWorkflowClient.ImportStateAsync(workflowState, cancellationToken);
    }
}