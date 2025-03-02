using Hangfire.Annotations;

namespace Elsa.Workflows.ComponentTests.Fixtures;

[UsedImplicitly]
public class Cluster(Infrastructure infrastructure) : IAsyncDisposable
{
    public WorkflowServer Pod3 { get; set; } = new(infrastructure, "http://localhost:5003");
    public WorkflowServer Pod2 { get; set; } = new(infrastructure, "http://localhost:5002");
    public WorkflowServer Pod1 { get; set; } = new(infrastructure, "http://localhost:5001");

    public async ValueTask DisposeAsync()
    {
        await Pod3.DisposeAsync();
        await Pod2.DisposeAsync();
        await Pod1.DisposeAsync();
    }
}