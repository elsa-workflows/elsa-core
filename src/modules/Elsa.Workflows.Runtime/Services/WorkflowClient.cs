using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Parameters;
using Elsa.Workflows.Runtime.Results;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime.Services;

public class WorkflowClient(string workflowInstanceId) : IWorkflowClient
{
    /// <inheritdoc />
    public string WorkflowInstanceId { get; set; } = workflowInstanceId;

    /// <inheritdoc />
    public async Task ExecuteAndWaitAsync(IExecuteWorkflowParams? @params = default, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task ExecuteAndForgetAsync(IExecuteWorkflowParams? @params = default, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task<CancellationResult> CancelAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task<WorkflowState> ExportStateAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task ImportStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}