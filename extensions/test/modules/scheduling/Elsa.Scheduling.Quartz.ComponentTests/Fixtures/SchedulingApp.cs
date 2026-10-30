using JetBrains.Annotations;

namespace Elsa.Scheduling.Quartz.ComponentTests.Fixtures;

[UsedImplicitly]
public class SchedulingApp : IAsyncLifetime
{
    public WorkflowServer WorkflowServer { get; } = new("http://localhost:5010");

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await WorkflowServer.DisposeAsync();
    }
}
