using Elsa.Workflows.ComponentTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Abstractions;

[Collection(nameof(AppCollection))]
public abstract class AppComponentTest : IDisposable
{
    protected AppComponentTest(App app)
    {
        WorkflowServer = app.WorkflowServer;
        Cluster = app.Cluster;
        Infrastructure = app.Infrastructure;
        Scope = app.WorkflowServer.Services.CreateScope();
    }

    protected WorkflowServer WorkflowServer { get; }
    protected Cluster Cluster { get; }
    protected Infrastructure Infrastructure { get; }
    protected IServiceScope Scope { get; }

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