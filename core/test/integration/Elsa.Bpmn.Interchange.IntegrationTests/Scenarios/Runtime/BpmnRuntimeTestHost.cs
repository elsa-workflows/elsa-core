using System.Collections.Concurrent;
using Bpmn.Interchange;
using Elsa.Bpmn.Interchange.IntegrationTests.Support;
using Elsa.Bpmn.Interchange.Services;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.HostedServices;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Notifications;
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
/// Nothing starts hosted services or recurring tasks in a test container, so the two that carry work off the caller's
/// thread are started here: the background command sender, which runs dispatches -- an asynchronous
/// <c>PublishEvent</c>, a <c>DispatchWorkflow</c> child -- and the bookmark queue worker, which a finished child resumes
/// its waiting parent through. The worker wakes on the signal every enqueue raises, as it does in a deployment. Work
/// that lands off the caller's thread is awaited through the <see cref="WorkflowInstanceSaved"/> notification the
/// runtime publishes on every save, never by polling the store.
/// </remarks>
public sealed class BpmnRuntimeTestHost
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(30);

    private readonly ITestOutputHelper _testOutputHelper;
    private readonly WorkflowInstanceSavedSignal _savedSignal = new();
    private readonly BackgroundCommandSenderHostedService _commandSender;
    private readonly AsyncServiceScope _bookmarkQueueScope;
    private readonly IBookmarkQueueWorker _bookmarkQueueWorker;
    private bool _bookmarkQueueWorkerStarted;

    public BpmnRuntimeTestHost(ITestOutputHelper testOutputHelper, Action<IModule>? configureElsa = null)
    {
        _testOutputHelper = testOutputHelper;
        Services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureServices(services => services.AddNotificationHandler<WorkflowInstanceSavedSignal, WorkflowInstanceSaved>(_ => _savedSignal))
            .ConfigureElsa(elsa =>
            {
                elsa.UseBpmnInterchange();
                configureElsa?.Invoke(elsa);
            })
            .Build();

        _commandSender = Services.GetServices<IHostedService>().OfType<BackgroundCommandSenderHostedService>().Single();
        _bookmarkQueueScope = Services.CreateAsyncScope();
        _bookmarkQueueWorker = _bookmarkQueueScope.ServiceProvider.GetRequiredService<IBookmarkQueueWorker>();
    }

    public IServiceProvider Services { get; }

    public async Task InitializeAsync()
    {
        await Services.PopulateRegistriesAsync();
        await _commandSender.StartAsync(CancellationToken.None);
        _bookmarkQueueWorker.Start();
        _bookmarkQueueWorkerStarted = true;
    }

    public async Task DisposeAsync()
    {
        try
        {
            if (_bookmarkQueueWorkerStarted)
            {
                _bookmarkQueueWorker.Stop();
            }

            await _commandSender.StopAsync(CancellationToken.None);
        }
        finally
        {
            await _bookmarkQueueScope.DisposeAsync();

            if (Services is IAsyncDisposable disposableServices)
            {
                await disposableServices.DisposeAsync();
            }
        }
    }

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

    /// <summary>Saves and publishes a workflow built in code under the given definition id, with optional workflow options.</summary>
    public async Task PublishAsync(string definitionId, IActivity root, WorkflowOptions? options = null)
    {
        var result = await Services.GetRequiredService<IWorkflowDefinitionImporter>().ImportAsync(new()
        {
            Model = new WorkflowDefinitionModel { DefinitionId = definitionId, Name = definitionId, Root = root, Options = options },
            Publish = true
        });

        Assert.True(result.Succeeded, string.Join("; ", result.ValidationErrors.Select(error => error.Message)));
    }

    /// <summary>Starts a new instance of the published definition, with <paramref name="input"/> as its input, and returns its id.</summary>
    public async Task<string> StartAsync(string definitionId, string? correlationId = null, IDictionary<string, object>? input = null)
    {
        var client = await Services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync();
        var response = await client.CreateAndRunInstanceAsync(new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Published),
            CorrelationId = correlationId,
            Input = input
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
    /// Waits until the instance is saved in a state that satisfies <paramref name="condition"/>, and returns that state.
    /// Times out by throwing, naming the last state observed.
    /// </summary>
    public async Task<WorkflowState> WaitForAsync(string instanceId, Func<WorkflowState, bool> condition)
    {
        try
        {
            var instance = await WaitForInstanceAsync(instance => instance.Id == instanceId && condition(instance.WorkflowState), $"Instance {instanceId}");
            return instance.WorkflowState;
        }
        catch (TimeoutException)
        {
            var state = await StateOfAsync(instanceId);
            _testOutputHelper.WriteLine($"Instance {instanceId} after {WaitTimeout}: {state.Status}/{state.SubStatus}, bookmarks [{string.Join(", ", state.Bookmarks.Select(bookmark => bookmark.ActivityId))}].");
            throw new TimeoutException($"Instance {instanceId} did not reach the expected state within {WaitTimeout}. Last observed: {state.Status}/{state.SubStatus}.");
        }
    }

    /// <summary>
    /// Waits until an instance matching <paramref name="match"/> is saved, or returns it straight away when one already
    /// is. Times out by throwing a <see cref="TimeoutException"/> that names <paramref name="description"/>.
    /// </summary>
    public async Task<WorkflowInstance> WaitForInstanceAsync(Func<WorkflowInstance, bool> match, string description)
    {
        // Watch before reading the store: a save that lands between the two is then caught by the watch, not lost.
        var saved = _savedSignal.Watch(match);

        try
        {
            var alreadySaved = (await Services.GetRequiredService<IWorkflowInstanceStore>().FindManyAsync(new WorkflowInstanceFilter())).FirstOrDefault(match);

            if (alreadySaved is not null)
            {
                return alreadySaved;
            }

            return await saved.Task.WaitAsync(WaitTimeout);
        }
        catch (TimeoutException)
        {
            throw new TimeoutException($"{description} was not saved in the expected state within {WaitTimeout}.");
        }
        finally
        {
            _savedSignal.Unwatch(saved);
        }
    }

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

    /// <summary>Completes each pending watch the first time an instance it matches is saved.</summary>
    private sealed class WorkflowInstanceSavedSignal : INotificationHandler<WorkflowInstanceSaved>
    {
        private readonly ConcurrentDictionary<TaskCompletionSource<WorkflowInstance>, Func<WorkflowInstance, bool>> _watches = new();

        public TaskCompletionSource<WorkflowInstance> Watch(Func<WorkflowInstance, bool> match)
        {
            var saved = new TaskCompletionSource<WorkflowInstance>(TaskCreationOptions.RunContinuationsAsynchronously);
            _watches[saved] = match;
            return saved;
        }

        public void Unwatch(TaskCompletionSource<WorkflowInstance> saved) => _watches.TryRemove(saved, out _);

        public Task HandleAsync(WorkflowInstanceSaved notification, CancellationToken cancellationToken)
        {
            foreach (var (saved, match) in _watches)
            {
                if (match(notification.WorkflowInstance))
                {
                    saved.TrySetResult(notification.WorkflowInstance);
                }
            }

            return Task.CompletedTask;
        }
    }
}
