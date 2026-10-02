using Elsa.Scheduling.Quartz.ComponentTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scheduling.Quartz.ComponentTests.Abstractions;

[Collection(nameof(SchedulingAppCollection))]
public abstract class AppComponentTest(SchedulingApp app) : IDisposable
{
    protected WorkflowServer WorkflowServer { get; } = app.WorkflowServer;
    protected IServiceScope Scope { get; private set; } = app.WorkflowServer.Services.CreateScope();

    public void Dispose()
    {
        OnDispose();
        Scope?.Dispose();
        GC.SuppressFinalize(this);
    }

    protected virtual void OnDispose()
    {
    }
}
