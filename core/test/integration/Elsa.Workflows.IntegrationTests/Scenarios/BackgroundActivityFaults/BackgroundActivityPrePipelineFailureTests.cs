using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Workflows.IntegrationTests.Scenarios.BackgroundActivityFaults;

/// <summary>
/// A background activity job can fail before the activity runs: the workflow instance or its definition is gone, or the
/// activity is no longer active. There is nothing in the workflow that could record these failures, so the job must fail
/// with a clear error and leave the stored workflow as it was.
/// </summary>
public class BackgroundActivityPrePipelineFailureTests : IAsyncLifetime
{
    private const string WaitActivityId = "Wait";

    private IHost _host = null!;
    private IServiceScope _scope = null!;
    private IServiceProvider _services = null!;

    public async Task InitializeAsync()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddElsa(elsa => elsa.UseWorkflowRuntime()))
            .Build();
        await _host.StartAsync();
        _scope = _host.Services.CreateScope();
        _services = _scope.ServiceProvider;
        await _services.PopulateRegistriesAsync();
    }

    public async Task DisposeAsync()
    {
        _scope.Dispose();
        using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await _host.StopAsync(stopTimeout.Token);
        _host.Dispose();
    }

    [Fact(DisplayName = "A background activity job for a missing workflow instance fails with a clear error")]
    public async Task MissingWorkflowInstance()
    {
        var job = new ScheduledBackgroundActivity("missing-instance", "missing-node", "missing-bookmark");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => ExecuteAsync(job));

        Assert.Contains("workflow instance missing-instance was not found", exception.Message);
        Assert.Contains("missing-node", exception.Message);
    }

    [Fact(DisplayName = "A background activity job whose workflow definition is missing fails with a clear error and leaves the workflow unchanged")]
    public async Task MissingWorkflowDefinition()
    {
        var (instanceId, nodeId) = await CreateSuspendedInstanceAsync(definitionVersionId: "missing-definition-version");
        var job = new ScheduledBackgroundActivity(instanceId, nodeId, "bookmark");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => ExecuteAsync(job));

        Assert.Contains("workflow definition version missing-definition-version was not found", exception.Message);
        Assert.Contains(instanceId, exception.Message);
        await AssertWorkflowUnchangedAsync(instanceId, nodeId);
    }

    [Fact(DisplayName = "A background activity job for an activity that is no longer active fails with a clear error and leaves the workflow unchanged")]
    public async Task ActivityNoLongerActive()
    {
        var (instanceId, nodeId) = await CreateSuspendedInstanceAsync();
        var job = new ScheduledBackgroundActivity(instanceId, "inactive-node", "bookmark");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => ExecuteAsync(job));

        Assert.Contains("inactive-node", exception.Message);
        Assert.Contains(instanceId, exception.Message);
        Assert.Contains("no longer active", exception.Message);
        await AssertWorkflowUnchangedAsync(instanceId, nodeId);
    }

    private async Task ExecuteAsync(ScheduledBackgroundActivity job)
    {
        using var scope = _host.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IBackgroundActivityInvoker>().ExecuteAsync(job);
    }

    /// <summary>
    /// Runs a workflow until it suspends at an event and stores it, optionally pointing it at another definition version.
    /// Returns the instance ID and the node ID of the suspended activity.
    /// </summary>
    private async Task<(string InstanceId, string NodeId)> CreateSuspendedInstanceAsync(string? definitionVersionId = null)
    {
        var workflow = await _services.GetRequiredService<IWorkflowBuilderFactory>().CreateBuilder()
            .BuildWorkflowAsync(new TestWorkflow(builder => builder.Root = new Sequence
            {
                Activities =
                {
                    new Event("Resume") { Id = WaitActivityId },
                    new WriteLine("Done")
                }
            }));
        await _services.GetRequiredService<IWorkflowRegistry>().RegisterAsync(workflow);

        var result = await _services.GetRequiredService<IWorkflowRunner>().RunAsync(workflow);
        var state = result.WorkflowState;
        Assert.Equal(WorkflowSubStatus.Suspended, state.SubStatus);

        if (definitionVersionId != null)
            state.DefinitionVersionId = definitionVersionId;

        await _services.GetRequiredService<IWorkflowInstanceManager>().SaveAsync(state, CancellationToken.None);
        var nodeId = state.ActivityExecutionContexts.Single(x => x.ScheduledActivityNodeId.EndsWith(WaitActivityId)).ScheduledActivityNodeId;
        return (state.Id, nodeId);
    }

    private async Task AssertWorkflowUnchangedAsync(string instanceId, string nodeId)
    {
        var instance = await _services.GetRequiredService<IWorkflowInstanceManager>().FindByIdAsync(instanceId);

        Assert.NotNull(instance);
        Assert.Equal(WorkflowSubStatus.Suspended, instance.SubStatus);
        Assert.Empty(instance.WorkflowState.Incidents);
        Assert.Contains(instance.WorkflowState.ActivityExecutionContexts, x => x.ScheduledActivityNodeId == nodeId && x.Status == ActivityStatus.Running);
        Assert.Single(instance.WorkflowState.Bookmarks);
    }
}
