using Elsa.Workflows.ComponentTests.Helpers.Fixtures;
using Hangfire.Annotations;
using JetBrains.Annotations;
using Xunit;

namespace Elsa.Workflows.ComponentTests;

[UsedImplicitly]
public class App : IAsyncLifetime
{
    public App()
    {
        Infrastructure = new();
        Cluster = new(Infrastructure);
        WorkflowServer = Cluster.Pod1;
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
        await Cluster.DisposeAsync();
        await Infrastructure.DisposeAsync();
    }
}