using Elsa.Workflows.ComponentTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Abstractions;

[Collection(nameof(AppCollection))]
public abstract class AppComponentTest(App app) : IDisposable
{
    protected WorkflowServer WorkflowServer { get; } = app.WorkflowServer;
    protected Cluster Cluster { get; } = app.Cluster;
    protected Infrastructure Infrastructure { get; } = app.Infrastructure;
    protected IServiceScope Scope { get; } = app.WorkflowServer.Services.CreateScope();

    void IDisposable.Dispose()
    {
        // Disposing the Scope here and in other places where it is created somehow seems to cause the test runner to hang when running other test projects.
        // Let's comment it out for the time being.
        //Scope.Dispose();
        OnDispose();
    }

    protected virtual void OnDispose()
    {
    }
}