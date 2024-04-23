using Hangfire.Annotations;

namespace Elsa.Workflows.ComponentTests;

[UsedImplicitly]
public class App : IAsyncLifetime
{
    public App()
    {
        Infrastructure = new();
        Cluster = new(Infrastructure);
        WorkflowServer = new(Infrastructure, "http://localhost:5000");
    }

    public Infrastructure Infrastructure { get; set; }
    public Cluster Cluster { get; set; }
    public WorkflowServer WorkflowServer { get; set; }

    public async Task InitializeAsync()
    {
        await Infrastructure.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await WorkflowServer.DisposeAsync();
        await Cluster.DisposeAsync();
        await Infrastructure.DisposeAsync();
    }
}