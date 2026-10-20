using Bpmn.Interchange;
using Elsa.Bpmn.Interchange.IntegrationTests.Support;
using Elsa.Bpmn.Interchange.Services;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Mediator.HostedServices;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;

namespace Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Runtime;

/// <summary>
/// An application that runs imported BPMN processes the way a deployed Elsa does: imported from a <c>.bpmn</c>
/// document, bound to real Elsa activities, published, started through <see cref="IWorkflowRuntime"/>, and driven by
/// the same stimuli a deployment would send.
/// </summary>
/// <remarks>
/// Nothing starts hosted services in a test container, so the one that carries dispatches -- an asynchronous
/// <c>PublishEvent</c>, a <c>DispatchWorkflow</c> child -- is started here, and the bookmark queue a finished child
/// resumes its waiting parent through is drained on every poll rather than left to its recurring task.
/// </remarks>
public sealed class BpmnRuntimeTestHost
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(30);

    private readonly ITestOutputHelper _testOutputHelper;
    private readonly BackgroundCommandSenderHostedService _commandSender;

    public BpmnRuntimeTestHost(ITestOutputHelper testOutputHelper, Action<IModule>? configureElsa = null)
    {
        _testOutputHelper = testOutputHelper;
        Services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa =>
            {
                elsa.UseBpmnInterchange();
                configureElsa?.Invoke(elsa);
            })
            .Build();

        _commandSender = Services.GetServices<IHostedService>().OfType<BackgroundCommandSenderHostedService>().Single();
    }

    public IServiceProvider Services { get; }

    public async Task InitializeAsync()
    {
        await Services.PopulateRegistriesAsync();
        await _commandSender.StartAsync(CancellationToken.None);
    }

    public Task DisposeAsync() => _commandSender.StopAsync(CancellationToken.None);

    /// <summary>The id of the Elsa activity the binder created for a BPMN element of the given process.</summary>
    public static string WorkActivityId(string processId, string elementId) => $"{processId}:{BpmnXmlReader.DefaultBindingRefPrefix}-{elementId}";

    /// <summary>Imports a <c>.bpmn</c> fixture through the document service and publishes it, returning its definition id.</summary>
    public async Task<string> ImportAndPublishAsync(string assetFileName)
    {
        var imported = await Services.GetRequiredService<BpmnInterchangeDocumentService>()
            .ImportAsync(BpmnAssetReader.Read(assetFileName), definitionId: null, name: null, processId: null, CancellationToken.None);

        Assert.True(imported.ImportResult.Succeeded, string.Join("; ", imported.ImportResult.ValidationErrors.Select(error => error.Message)));

        var definitionId = imported.ImportResult.WorkflowDefinition.DefinitionId;
        await DefinitionPublishing.PublishLatestAsync(Services.GetRequiredService<IWorkflowDefinitionPublisher>(), definitionId);

        return definitionId;
    }

    /// <summary>Saves and publishes a workflow built in code under the given definition id.</summary>
    public async Task PublishAsync(string definitionId, IActivity root)
    {
        var result = await Services.GetRequiredService<IWorkflowDefinitionImporter>().ImportAsync(new()
        {
            Model = new WorkflowDefinitionModel { DefinitionId = definitionId, Name = definitionId, Root = root },
            Publish = true
        });

        Assert.True(result.Succeeded, string.Join("; ", result.ValidationErrors.Select(error => error.Message)));
    }

    /// <summary>Starts a new instance of the published definition and returns its id.</summary>
    public async Task<string> StartAsync(string definitionId, string? correlationId = null)
    {
        var client = await Services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync();
        var response = await client.CreateAndRunInstanceAsync(new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Published),
            CorrelationId = correlationId
        });

        return response.WorkflowInstanceId;
    }

    /// <summary>Resumes the instance's bookmark held by the given activity, the way a scheduler fires a due timer.</summary>
    public async Task ResumeAsync(string instanceId, string activityId)
    {
        var bookmark = (await StateOfAsync(instanceId)).Bookmarks.Single(bookmark => bookmark.ActivityId == activityId);
        var client = await Services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync(instanceId);

        await client.RunInstanceAsync(new RunWorkflowInstanceRequest { BookmarkId = bookmark.Id });
    }

    /// <summary>The instance's persisted state.</summary>
    public async Task<WorkflowState> StateOfAsync(string instanceId)
    {
        var instance = await Services.GetRequiredService<IWorkflowInstanceStore>().FindAsync(new WorkflowInstanceFilter { Id = instanceId });

        return Assert.IsType<WorkflowInstance>(instance).WorkflowState;
    }

    /// <summary>
    /// Polls the instance until it satisfies <paramref name="condition"/>, draining the bookmark queue on every pass,
    /// and returns its state. Times out by throwing, naming the last state observed.
    /// </summary>
    public async Task<WorkflowState> WaitForAsync(string instanceId, Func<WorkflowState, bool> condition)
    {
        var timeoutAt = DateTimeOffset.UtcNow + WaitTimeout;
        WorkflowState state;

        do
        {
            using (var scope = Services.CreateScope())
                await scope.ServiceProvider.GetRequiredService<IBookmarkQueueProcessor>().ProcessAsync();

            state = await StateOfAsync(instanceId);

            if (condition(state))
                return state;

            await Task.Delay(TimeSpan.FromMilliseconds(50));
        } while (DateTimeOffset.UtcNow < timeoutAt);

        _testOutputHelper.WriteLine($"Instance {instanceId} after {WaitTimeout}: {state.Status}/{state.SubStatus}, bookmarks [{string.Join(", ", state.Bookmarks.Select(bookmark => bookmark.ActivityId))}].");
        throw new TimeoutException($"Instance {instanceId} did not reach the expected state within {WaitTimeout}. Last observed: {state.Status}/{state.SubStatus}.");
    }

    /// <summary>The instances the given definition has, newest last.</summary>
    public async Task<IReadOnlyList<WorkflowInstance>> InstancesOfAsync(string definitionId) =>
        (await Services.GetRequiredService<IWorkflowInstanceStore>().FindManyAsync(new WorkflowInstanceFilter { DefinitionId = definitionId }))
        .OrderBy(instance => instance.CreatedAt)
        .ToList();

    /// <summary>
    /// The status each activity of the instance last reached, keyed by activity id: which work ran, which was torn
    /// down, and -- by absence -- which never started.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, ActivityStatus>> ActivityStatusesOfAsync(string instanceId)
    {
        var records = await Services.GetRequiredService<IActivityExecutionStore>().FindManyAsync(new ActivityExecutionRecordFilter { WorkflowInstanceId = instanceId });

        return records
            .GroupBy(record => record.ActivityId)
            .ToDictionary(group => group.Key, group => group.OrderBy(record => record.StartedAt).Last().Status);
    }

    /// <summary>How many times each activity of the instance ran, keyed by activity id.</summary>
    public async Task<IReadOnlyDictionary<string, int>> ExecutionCountsOfAsync(string instanceId)
    {
        var records = await Services.GetRequiredService<IActivityExecutionStore>().FindManyAsync(new ActivityExecutionRecordFilter { WorkflowInstanceId = instanceId });

        return records.GroupBy(record => record.ActivityId).ToDictionary(group => group.Key, group => group.Count());
    }

    /// <summary>Publishes an event the way an application does, optionally scoped to a correlation id.</summary>
    public Task PublishEventAsync(string eventName, string? correlationId = null) =>
        Services.GetRequiredService<IEventPublisher>().PublishAsync(eventName, correlationId);

    public static bool IsSuspended(WorkflowState state) => state.SubStatus == WorkflowSubStatus.Suspended;

    public static bool IsFinished(WorkflowState state) => state.Status == WorkflowStatus.Finished;
}
