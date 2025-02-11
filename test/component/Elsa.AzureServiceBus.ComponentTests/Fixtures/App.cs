namespace Elsa.AzureServiceBus.ComponentTests;

public class App : IAsyncLifetime
{
    public App()
    {
        Infrastructure = new();
        WorkflowServer = new(Infrastructure, "http://localhost:5004");
    }

    public Infrastructure Infrastructure { get; set; }
    public WorkflowServer WorkflowServer { get; set; }

    public async Task InitializeAsync()
    {
        await Infrastructure.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await Infrastructure.DisposeAsync();
        await WorkflowServer.DisposeAsync();
    }
}