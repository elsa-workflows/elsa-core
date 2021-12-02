namespace Elsa.Activities.Mqtt.Testing
{
    public interface IMqttTestClientManager
    {
        Task CreateTestWorkersAsync(string workflowId, string workflowInstanceId, CancellationToken cancellationToken = default);
        Task DisposeTestWorkersAsync(string workflowInstanceId);
    }
}
