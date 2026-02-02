using Elsa.Common.Multitenancy;
using Elsa.Workflows.ComponentTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Abstractions;

[Collection(nameof(AppCollection))]
public abstract class AppComponentTest : IDisposable
{
    protected WorkflowServer WorkflowServer { get; }
    protected Cluster Cluster { get; }
    protected Infrastructure Infrastructure { get; }
    protected IServiceScope Scope { get; }
    private readonly IDisposable _tenantScope;

    protected AppComponentTest(App app)
    {
        WorkflowServer = app.WorkflowServer;
        Cluster = app.Cluster;
        Infrastructure = app.Infrastructure;
        Scope = app.WorkflowServer.Services.CreateScope();

        var tenantAccessor = Scope.ServiceProvider.GetRequiredService<ITenantAccessor>();
        _tenantScope = tenantAccessor.PushContext(new Tenant { Id = string.Empty, Name = "Default" });
    }

    void IDisposable.Dispose()
    {
        _tenantScope.Dispose();
        Scope.Dispose();
        OnDispose();
    }

    protected virtual void OnDispose()
    {
    }
}