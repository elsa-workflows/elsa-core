using System.Collections.Concurrent;
using Elsa.Common.Multitenancy;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.Services;
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
    private readonly WorkflowEvents _workflowEvents;
    private readonly ManualResetEventSlim _allWorkflowsIdle = new(true); // starts signaled (no workflows running)
    private readonly ConcurrentDictionary<string, byte> _runningWorkflowIds = new();

    protected AppComponentTest(App app)
    {
        WorkflowServer = app.WorkflowServer;
        Cluster = app.Cluster;
        Infrastructure = app.Infrastructure;
        Scope = app.WorkflowServer.Services.CreateScope();

        var tenantAccessor = Scope.ServiceProvider.GetRequiredService<ITenantAccessor>();
        _tenantScope = tenantAccessor.PushContext(new Tenant { Id = string.Empty, Name = "Default" });

        // Subscribe to workflow lifecycle events to track in-flight workflows.
        _workflowEvents = app.WorkflowServer.Services.GetRequiredService<WorkflowEvents>();
        _workflowEvents.WorkflowStateCommitted += OnWorkflowStateCommitted;
    }

    void IDisposable.Dispose()
    {
        // Wait for all workflows to reach terminal state before disposing scope.
        // This prevents TaskCanceledException when workflows are still executing.
        WaitForWorkflowsToComplete();

        _workflowEvents.WorkflowStateCommitted -= OnWorkflowStateCommitted;
        _allWorkflowsIdle.Dispose();
        _tenantScope.Dispose();
        Scope.Dispose();
        OnDispose();
    }

    protected virtual void OnDispose()
    {
    }

    private void OnWorkflowStateCommitted(object? sender, WorkflowStateCommittedEventArgs e)
    {
        var instanceId = e.WorkflowExecutionContext.Id;
        var status = e.WorkflowExecutionContext.Status;

        if (status == WorkflowStatus.Running)
        {
            // Track this instance as in-flight.
            if (_runningWorkflowIds.TryAdd(instanceId, 0) && _runningWorkflowIds.Count == 1)
                _allWorkflowsIdle.Reset();
        }
        else
        {
            // Workflow reached a terminal state; remove it.
            _runningWorkflowIds.TryRemove(instanceId, out _);

            if (_runningWorkflowIds.IsEmpty)
                _allWorkflowsIdle.Set();
        }
    }

    private void WaitForWorkflowsToComplete()
    {
        try
        {
            // Wait up to 10 seconds for all tracked workflows to finish.
            // If no workflows were started, the event is already signaled and this returns immediately.
            _allWorkflowsIdle.Wait(TimeSpan.FromSeconds(10));
        }
        catch
        {
            // Swallow exceptions during cleanup to avoid masking test failures.
        }
    }
}