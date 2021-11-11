namespace Elsa.WorkflowTesting.Services
{
    public interface IRabbitMqTestQueueManager
    {
        Task CreateTestWorkersAsync(string workflowId, string workflowInstanceId, CancellationToken cancellationToken = default);
        Task DisposeTestWorkersAsync(string workflowInstanceId);
    }
}