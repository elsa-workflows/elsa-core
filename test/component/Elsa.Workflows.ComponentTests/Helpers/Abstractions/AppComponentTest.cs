using Elsa.Common.Multitenancy;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
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
        // Wait for all workflows to reach terminal state before disposing scope
        // This prevents TaskCanceledException when workflows are still executing
        WaitForWorkflowsToComplete();

        _tenantScope.Dispose();
        Scope.Dispose();
        OnDispose();
    }

    protected virtual void OnDispose()
    {
    }

    private void WaitForWorkflowsToComplete()
    {
        try
        {
            var workflowInstanceStore = Scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
            var timeout = TimeSpan.FromSeconds(10);
            var pollInterval = TimeSpan.FromMilliseconds(50);
            var deadline = DateTime.UtcNow.Add(timeout);

            while (DateTime.UtcNow < deadline)
            {
                var filter = new WorkflowInstanceFilter
                {
                    WorkflowStatus = WorkflowStatus.Running
                };

                // Use async method synchronously - acceptable in cleanup/dispose
                var runningWorkflows = workflowInstanceStore.FindManyAsync(filter, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();

                if (!runningWorkflows.Any())
                    return; // All workflows completed

                Thread.Sleep(pollInterval);
            }

            // If we reach here, workflows didn't complete in time
            // Log but don't throw to avoid masking actual test failures
        }
        catch
        {
            // Swallow exceptions during cleanup to avoid masking test failures
        }
    }
}