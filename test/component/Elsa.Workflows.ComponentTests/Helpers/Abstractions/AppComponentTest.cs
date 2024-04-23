using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests;

[Collection(nameof(AppCollection))]
public abstract class AppComponentTest(App app) : IDisposable
{
    protected WorkflowServer WorkflowServer { get; } = app.WorkflowServer;
    protected Cluster Cluster { get; } = app.Cluster;
    protected Infrastructure Infrastructure { get; } = app.Infrastructure;
    protected IServiceScope Scope { get; } = app.WorkflowServer.Services.CreateScope();

    protected virtual void OnDispose()
    {
    }

    void IDisposable.Dispose()
    {
        Scope.Dispose();
        OnDispose();
    }
}