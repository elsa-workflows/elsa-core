using Elsa.Common;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.IntegrationTests.GracefulShutdown;

/// <summary>
/// Integration tests for the <see cref="RestartInterruptedWorkflowsTask" />
/// </summary>
public class RestartInterruptedWorkflowsTests : IAsyncDisposable
{
    private readonly IServiceProvider _services;

    public RestartInterruptedWorkflowsTests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            .ConfigureElsa(elsa => elsa.UseWorkflowRuntime())
            .Build();
    }

    [Test]
    [DisplayName("Task ignores instances NOT Interrupted")]
    public async Task Filter()
    {
        var fakeRestarter = new RecordingRestarter();
        using var scope = _services.CreateScope();
        var instanceStore = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
        var staleTimestamp = GetStaleTimestamp(scope.ServiceProvider);

        await SeedInstancesAsync(instanceStore, 3, WorkflowStatus.Running, WorkflowSubStatus.Executing, isExecuting: true, idPrefix: "stale-", timestamp: staleTimestamp);
        await SeedInstancesAsync(instanceStore, 2, WorkflowStatus.Finished, WorkflowSubStatus.Executing, isExecuting: true, idPrefix: "finished-stale-", timestamp: staleTimestamp);
        await SeedInstancesAsync(instanceStore, 1, WorkflowStatus.Finished, WorkflowSubStatus.Executing, isExecuting: false, idPrefix: "finished-", timestamp: staleTimestamp);

        var scanner = ActivatorUtilities.CreateInstance<RestartInterruptedWorkflowsTask>(scope.ServiceProvider, fakeRestarter);
        await scanner.ExecuteAsync(CancellationToken.None);

        await Assert.That(fakeRestarter.RestartedIds.Count).IsEqualTo(3);
        foreach (var id in fakeRestarter.RestartedIds)
            await Assert.That(id).StartsWith("stale-", StringComparison.CurrentCulture);
    }

    private static DateTimeOffset GetStaleTimestamp(IServiceProvider serviceProvider)
    {
        var clock = serviceProvider.GetRequiredService<ISystemClock>();
        var options = serviceProvider.GetRequiredService<IOptions<RuntimeOptions>>().Value;
        return clock.UtcNow - options.InactivityThreshold - TimeSpan.FromMinutes(1);
    }

    private static async Task SeedInstancesAsync(IWorkflowInstanceStore store, int count, WorkflowStatus status, WorkflowSubStatus subStatus, bool isExecuting, string idPrefix = "instance-", DateTimeOffset? timestamp = null)
    {
        var createdAt = timestamp ?? DateTimeOffset.UtcNow;

        for (var i = 0; i < count; i++)
        {
            await store.SaveAsync(new WorkflowInstance
            {
                Id = $"{idPrefix}{i}",
                DefinitionId = "def-1",
                DefinitionVersionId = "ver-1",
                Version = 1,
                Status = status,
                SubStatus = subStatus,
                IsExecuting = isExecuting,
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
                WorkflowState = new State.WorkflowState
                {
                    Id = $"{idPrefix}{i}",
                    DefinitionId = "def-1",
                    DefinitionVersionId = "ver-1",
                    Status = status,
                    SubStatus = subStatus,
                },
            }, CancellationToken.None);
        }
    }

    /// <summary>Captures restart calls without actually invoking the workflow runtime — keeps the integration test focused.</summary>
    private sealed class RecordingRestarter : IWorkflowRestarter
    {
        public List<string> RestartedIds { get; } = [];

        public Task RestartWorkflowAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
        {
            RestartedIds.Add(workflowInstanceId);
            return Task.CompletedTask;
        }
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
