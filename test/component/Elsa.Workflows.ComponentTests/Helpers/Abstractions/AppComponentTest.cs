using Elsa.Common.Multitenancy;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Abstractions;

/// <summary>
/// Supplies a fresh application, catalog, DI graph, and mutable filesystem root to every expanded test invocation.
/// </summary>
[ClassDataSource<App>(Shared = SharedType.None)]
public abstract class AppComponentTest : IAsyncDisposable
{
    private readonly App _app;
    private readonly WorkflowExecutionTracker _workflowExecutionTracker;
    private IDisposable? _tenantScope;
    private AsyncServiceScope? _scope;
    private Cluster? _cluster;
    private WorkflowServer? _workflowServer;
    private int _disposed;

    protected AppComponentTest(App app)
    {
        _app = app;
        _workflowExecutionTracker = app.HostOptions.WorkflowExecutionTracker;
        Infrastructure = app.Infrastructure;
    }

    protected WorkflowServer WorkflowServer => _workflowServer
        ?? throw new InvalidOperationException("The component-test host has not been initialized.");

    protected Cluster Cluster => _cluster
        ?? throw new InvalidOperationException("The component-test cluster has not been initialized.");

    protected Infrastructure Infrastructure { get; }

    protected AsyncServiceScope Scope => _scope
        ?? throw new InvalidOperationException("The component-test service scope has not been initialized.");

    [Before(Test)]
    public async Task InitializeComponentTestAsync(TestContext testContext)
    {
        _cluster = await _app.StartAsync(testContext);
        _workflowServer = _cluster.Pod1;
        _scope = _workflowServer.Services.CreateAsyncScope();

        var tenantAccessor = Scope.ServiceProvider.GetRequiredService<ITenantAccessor>();
        _tenantScope = tenantAccessor.PushContext(new Tenant { Id = string.Empty, Name = "Default" });

        await OnInitializeAsync();
    }

    // TUnit currently suppresses exceptions thrown from test-instance IAsyncDisposable cleanup.
    // Running the same idempotent path as an After(Test) hook makes cleanup failures part of the test result.
    [After(Test)]
    public async Task CleanupAsync()
    {
        List<Exception>? failures = null;

        try
        {
            await DisposeAsync();
        }
        catch (Exception exception)
        {
            (failures ??= []).Add(exception);
        }

        try
        {
            await _app.DisposeAsync();
        }
        catch (Exception exception)
        {
            (failures ??= []).Add(exception);
        }

        if (failures is { Count: > 0 })
            throw new AggregateException("Failed to clean up a component-test invocation.", failures);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        List<Exception>? failures = null;

        try
        {
            await WaitForWorkflowsToCompleteAsync();
        }
        catch (Exception exception)
        {
            (failures ??= []).Add(exception);
        }

        try
        {
            await OnDisposeAsync();
        }
        catch (Exception exception)
        {
            (failures ??= []).Add(exception);
        }

        try
        {
            _tenantScope?.Dispose();
        }
        catch (Exception exception)
        {
            (failures ??= []).Add(exception);
        }
        finally
        {
            _tenantScope = null;
        }

        if (_scope is { } scope)
        {
            try
            {
                await scope.DisposeAsync();
            }
            catch (Exception exception)
            {
                (failures ??= []).Add(exception);
            }
            finally
            {
                _scope = null;
            }
        }

        if (_cluster is not null)
        {
            try
            {
                await _cluster.DisposeAsync();
            }
            catch (Exception exception)
            {
                (failures ??= []).Add(exception);
            }
            finally
            {
                _cluster = null;
                _workflowServer = null;
            }
        }

        if (failures is { Count: > 0 })
            throw new AggregateException("Failed to dispose a component-test instance cleanly.", failures);
    }

    protected virtual ValueTask OnInitializeAsync() => ValueTask.CompletedTask;

    protected virtual ValueTask OnDisposeAsync() => ValueTask.CompletedTask;

    private async Task WaitForWorkflowsToCompleteAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            timeout.Token,
            TestContext.Current?.Execution.CancellationToken ?? CancellationToken.None);

        try
        {
            await _workflowExecutionTracker.WaitForIdleAsync(linked.Token);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            throw new TimeoutException($"Workflow executions did not become idle within 10 seconds; {_workflowExecutionTracker.ActiveCount} runner call(s) remain active.");
        }
    }
}
