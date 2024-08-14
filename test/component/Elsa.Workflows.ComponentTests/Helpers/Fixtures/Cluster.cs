using Hangfire.Annotations;

namespace Elsa.Workflows.ComponentTests.Helpers;

[UsedImplicitly]
public class Cluster : IDisposable, IAsyncDisposable
{
    public Cluster(Infrastructure infrastructure)
    {
        Pod1 = new(infrastructure, "http://localhost:5001");
        Pod2 = new(infrastructure, "http://localhost:5002");
        Pod3 = new(infrastructure, "http://localhost:5003");
    }

    public WorkflowServer Pod3 { get; set; }
    public WorkflowServer Pod2 { get; set; }
    public WorkflowServer Pod1 { get; set; }

    public void Dispose()
    {
        Pod3.Dispose();
        Pod2.Dispose();
        Pod1.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await Pod3.DisposeAsync();
        await Pod2.DisposeAsync();
        await Pod1.DisposeAsync();
    }
}