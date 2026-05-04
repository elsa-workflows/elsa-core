using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.GracefulShutdown;

/// <summary>
/// Integration tests for the <see cref="RestartInterruptedWorkflowsTask" />
/// </summary>
public class RestartInterruptedWorkflowsTests
{
    private readonly IServiceProvider _services;

    public RestartInterruptedWorkflowsTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa => elsa.UseWorkflowRuntime())
            .Build();
    }

    [Fact(DisplayName = "Task ignores instances NOT Interrupted")]
    public async Task Filter()
    {
        var fakeRestarter = new RecordingRestarter();
        using var scope = _services.CreateScope();
        var instanceStore = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();

        await SeedInstancesAsync(instanceStore, 3, WorkflowStatus.Running, WorkflowSubStatus.Executing, isExecuting: true, idPrefix: "stale-");
        await SeedInstancesAsync(instanceStore, 2, WorkflowStatus.Finished, WorkflowSubStatus.Executing, isExecuting: true, idPrefix: "finished-stale-");
        await SeedInstancesAsync(instanceStore, 1, WorkflowStatus.Finished, WorkflowSubStatus.Executing, isExecuting: false, idPrefix: "finished-");

        var scanner = ActivatorUtilities.CreateInstance<RestartInterruptedWorkflowsTask>(scope.ServiceProvider, fakeRestarter);
        await scanner.ExecuteAsync(CancellationToken.None);

        Assert.Equal(3, fakeRestarter.RestartedIds.Count);
        Assert.All(fakeRestarter.RestartedIds, id => Assert.StartsWith("stale-", id));
    }

    private static async Task SeedInstancesAsync(IWorkflowInstanceStore store, int count, WorkflowStatus status, WorkflowSubStatus subStatus, bool isExecuting, string idPrefix = "instance-")
    {
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
                CreatedAt = DateTimeOffset.Parse("2026-04-13T12:00:00Z"),
                UpdatedAt = DateTimeOffset.Parse("2026-04-13T12:00:00Z"),
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
}
