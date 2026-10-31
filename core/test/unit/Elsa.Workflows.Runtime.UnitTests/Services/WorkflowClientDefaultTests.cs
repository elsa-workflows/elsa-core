using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class WorkflowClientDefaultTests
{
    [Fact]
    public async Task RunInstanceIfPendingAsync_UsesTheClientSideGuardByDefault()
    {
        var client = new DefaultClient(WorkflowSubStatus.Pending);

        var response = await ((IWorkflowClient)client).RunInstanceIfPendingAsync(new RunWorkflowInstanceRequest());

        Assert.NotNull(response);
        Assert.Equal(1, client.ExportCount);
        Assert.Equal(1, client.RunCount);
    }

    [Theory]
    [InlineData(WorkflowSubStatus.Executing)]
    [InlineData(WorkflowSubStatus.Finished)]
    [InlineData(WorkflowSubStatus.Faulted)]
    public async Task RunInstanceIfPendingAsync_ReturnsNullWithoutRunningWhenNotPending(WorkflowSubStatus subStatus)
    {
        var client = new DefaultClient(subStatus);

        var response = await ((IWorkflowClient)client).RunInstanceIfPendingAsync(new RunWorkflowInstanceRequest());

        Assert.Null(response);
        Assert.Equal(1, client.ExportCount);
        Assert.Equal(0, client.RunCount);
    }

    private sealed class DefaultClient(WorkflowSubStatus subStatus) : IWorkflowClient
    {
        public string WorkflowInstanceId => "test-instance";
        public int ExportCount { get; private set; }
        public int RunCount { get; private set; }

        public Task<CreateWorkflowInstanceResponse> CreateInstanceAsync(CreateWorkflowInstanceRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<RunWorkflowInstanceResponse> RunInstanceAsync(RunWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
        {
            RunCount++;
            return Task.FromResult(new RunWorkflowInstanceResponse { WorkflowInstanceId = WorkflowInstanceId });
        }

        public Task<RunWorkflowInstanceResponse> CreateAndRunInstanceAsync(CreateAndRunWorkflowInstanceRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task CancelAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<WorkflowState> ExportStateAsync(CancellationToken cancellationToken = default)
        {
            ExportCount++;
            return Task.FromResult(new WorkflowState { SubStatus = subStatus });
        }

        public Task ImportStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> InstanceExistsAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<bool> DeleteAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
    }
}
