using Bunit;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.DomInterop.Models;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.Shared.Components;
using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.UI.Contexts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public sealed class WorkflowEditorLifecycleTests : BunitContext, IAsyncLifetime
{
    private readonly DelayedWorkflowDefinitionEditorService _editorService = new();
    private readonly RecordingUserMessageService _userMessageService = new();

    public WorkflowEditorLifecycleTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddLogging();
        Services.AddCoreInternal();
        Services.AddRemoteBackend();
        Services.AddWorkflowsModule();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<IWorkflowDefinitionEditorService>(_editorService);
        Services.AddSingleton<IActivityRegistry, NoOpActivityRegistry>();
        Services.AddSingleton<IDomAccessor, NoOpDomAccessor>();
        Services.AddSingleton(_userMessageService);
        Services.AddSingleton<IUserMessageService>(_userMessageService);
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public async Task SaveSuccessCompletedAfterInvalidationDoesNotReplaceOrInvokeSuccess()
    {
        var originalDefinition = CreateDefinition("initial");
        var replacementCount = 0;
        var cut = RenderEditor(originalDefinition, () =>
        {
            replacementCount++;
            return Task.CompletedTask;
        });
        var successCount = 0;

        var saveTask = InvokeSaveAsync(cut.Instance, _ =>
        {
            successCount++;
            return Task.CompletedTask;
        }, null);
        var pendingSave = await _editorService.SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        cut.Instance.InvalidateWorkflowDefinitionOperations();
        await pendingSave.CompleteSuccessAsync(CreateDefinition("server response"));
        await saveTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Same(originalDefinition, cut.Instance.WorkflowDefinition);
        Assert.True(pendingSave.CallbackInvoked);
        Assert.Equal(0, replacementCount);
        Assert.Equal(0, successCount);
    }

    [Fact]
    public async Task DiagramLoadCompletionLeavesTheLatestWorkflowSelected()
    {
        var activityVisitor = new DelayedActivityVisitor();
        Services.AddSingleton<IActivityVisitor>(activityVisitor);
        Services.AddSingleton<IDiagramDesignerService, TestDiagramDesignerService>();

        var initialDefinition = CreateDefinition("initial");
        var firstDefinition = CreateDefinition("first");
        var secondDefinition = CreateDefinition("second");
        var firstLoad = activityVisitor.Enqueue();
        var cut = RenderEditor(initialDefinition, () => Task.CompletedTask);

        Task? firstTask = null;
        await cut.InvokeAsync(() =>
        {
            SetWorkflowDefinition(cut.Instance, firstDefinition);
            firstTask = InvokeOnParametersSetAsync(cut.Instance);
        });
        await firstLoad.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var secondLoad = activityVisitor.Enqueue();
        Task? secondTask = null;
        await cut.InvokeAsync(() =>
        {
            SetWorkflowDefinition(cut.Instance, secondDefinition);
            secondTask = InvokeOnParametersSetAsync(cut.Instance);
        });

        if (secondLoad.Started.Task.IsCompleted)
        {
            await cut.InvokeAsync(secondLoad.Complete);
            await secondTask!.WaitAsync(TimeSpan.FromSeconds(5));
            await cut.InvokeAsync(firstLoad.Complete);
        }
        else
        {
            await cut.InvokeAsync(firstLoad.Complete);
            await secondLoad.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await cut.InvokeAsync(secondLoad.Complete);
        }

        await firstTask!.WaitAsync(TimeSpan.FromSeconds(5));
        await secondTask!.WaitAsync(TimeSpan.FromSeconds(5));

        var diagram = cut.FindComponent<DiagramDesignerWrapper>().Instance;
        var activityGraph = await diagram.GetActivityGraphAsync();
        Assert.Same(secondDefinition.Root, diagram.Activity);
        Assert.Same(secondDefinition.Root, activityGraph.Activity);
        Assert.Equal(secondDefinition.Root!["id"]!.GetValue<string>(), cut.Instance.SelectedActivityId);
    }

    [Fact]
    public async Task ObsoleteDiagramLoadFailureDoesNotPreventTheLatestLoad()
    {
        var activityVisitor = new DelayedActivityVisitor();
        Services.AddSingleton<IActivityVisitor>(activityVisitor);
        Services.AddSingleton<IDiagramDesignerService, TestDiagramDesignerService>();

        var initialDefinition = CreateDefinition("initial");
        var firstDefinition = CreateDefinition("first");
        var secondDefinition = CreateDefinition("second");
        var firstLoad = activityVisitor.Enqueue();
        var cut = RenderEditor(initialDefinition, () => Task.CompletedTask);

        Task? firstTask = null;
        await cut.InvokeAsync(() =>
        {
            SetWorkflowDefinition(cut.Instance, firstDefinition);
            firstTask = InvokeOnParametersSetAsync(cut.Instance);
        });
        await firstLoad.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var secondLoad = activityVisitor.Enqueue();
        Task? secondTask = null;
        await cut.InvokeAsync(() =>
        {
            SetWorkflowDefinition(cut.Instance, secondDefinition);
            secondTask = InvokeOnParametersSetAsync(cut.Instance);
        });

        firstLoad.Fail(new InvalidOperationException("obsolete diagram load"));
        await firstTask!.WaitAsync(TimeSpan.FromSeconds(5));
        await secondLoad.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        secondLoad.Complete();
        await secondTask!.WaitAsync(TimeSpan.FromSeconds(5));

        var diagram = cut.FindComponent<DiagramDesignerWrapper>().Instance;
        var activityGraph = await diagram.GetActivityGraphAsync();
        Assert.Same(secondDefinition.Root, diagram.Activity);
        Assert.Same(secondDefinition.Root, activityGraph.Activity);
    }

    [Fact]
    public async Task OlderForegroundSaveCannotClearAConcurrentProgressOwner()
    {
        var originalDefinition = CreateDefinition("initial");
        var cut = RenderEditor(originalDefinition, () => Task.CompletedTask);
        var firstSaveTask = InvokeSaveWithLoaderAsync(cut.Instance);
        var firstSave = await _editorService.SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var secondSaveTask = InvokeSaveWithLoaderAsync(cut.Instance);
        var secondSave = await _editorService.SecondSaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(GetIsProgressing(cut.Instance));

        await firstSave.CompleteSuccessAsync(CreateDefinition("first response"));
        await firstSaveTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(GetIsProgressing(cut.Instance));

        await secondSave.CompleteSuccessAsync(CreateDefinition("second response"));
        await secondSaveTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(GetIsProgressing(cut.Instance));
    }

    [Fact]
    public async Task ObsoleteForegroundSaveCompletionAfterDisposeDoesNotRender()
    {
        var cut = RenderEditor(CreateDefinition("initial"), () => Task.CompletedTask);
        var saveTask = InvokeSaveWithLoaderAsync(cut.Instance);
        var pendingSave = await _editorService.SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var renderCount = cut.RenderCount;

        cut.Instance.Dispose();
        await pendingSave.CompleteSuccessAsync(CreateDefinition("server response"));
        await saveTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(renderCount, cut.RenderCount);
    }

    [Fact]
    public async Task SaveFailureCompletedAfterInvalidationDoesNotInvokeFailure()
    {
        var originalDefinition = CreateDefinition("initial");
        var cut = RenderEditor(originalDefinition, () => Task.CompletedTask);
        var failureCount = 0;

        var saveTask = InvokeSaveAsync(cut.Instance, null, _ =>
        {
            failureCount++;
            return Task.CompletedTask;
        });
        var pendingSave = await _editorService.SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        cut.Instance.InvalidateWorkflowDefinitionOperations();
        pendingSave.CompleteFailure();
        await saveTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(0, failureCount);
        Assert.Equal(0, _userMessageService.MessageCount);
    }

    [Fact]
    public async Task RetractSuccessCompletedAfterInvalidationDoesNotReplaceOrInvokeSuccess()
    {
        var originalDefinition = CreateDefinition("initial");
        var replacementCount = 0;
        var cut = RenderEditor(originalDefinition, () =>
        {
            replacementCount++;
            return Task.CompletedTask;
        });
        var successCount = 0;

        var retractTask = InvokeRetractAsync(cut.Instance, () =>
        {
            successCount++;
            return Task.CompletedTask;
        }, null);
        var pendingRetract = await _editorService.RetractStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        cut.Instance.InvalidateWorkflowDefinitionOperations();
        await pendingRetract.CompleteSuccessAsync(CreateDefinition("retracted response"));
        await retractTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Same(originalDefinition, cut.Instance.WorkflowDefinition);
        Assert.True(pendingRetract.CallbackInvoked);
        Assert.Equal(0, replacementCount);
        Assert.Equal(0, successCount);
    }

    [Fact]
    public async Task RetractFailureCompletedAfterInvalidationDoesNotInvokeFailure()
    {
        var originalDefinition = CreateDefinition("initial");
        var cut = RenderEditor(originalDefinition, () => Task.CompletedTask);
        var failureCount = 0;

        var retractTask = InvokeRetractAsync(cut.Instance, null, _ =>
        {
            failureCount++;
            return Task.CompletedTask;
        });
        var pendingRetract = await _editorService.RetractStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        cut.Instance.InvalidateWorkflowDefinitionOperations();
        pendingRetract.CompleteFailure();
        await retractTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(0, failureCount);
    }

    private IRenderedComponent<WorkflowEditor> RenderEditor(WorkflowDefinition definition, Func<Task> workflowDefinitionUpdated)
    {
        ComponentFactories.Add<DiagramDesignerWrapper, TestDiagramDesignerWrapper>();
        ComponentFactories.Add<ActivityPropertiesPanel, TestActivityPropertiesPanel>();
        return Render<WorkflowEditor>(parameters => parameters
            .Add(x => x.WorkflowDefinition, definition)
            .Add(x => x.WorkflowDefinitionUpdated, workflowDefinitionUpdated));
    }

    private static Task InvokeSaveAsync(WorkflowEditor editor, Func<SaveWorkflowDefinitionResponse, Task>? onSuccess, Func<ValidationErrors, Task>? onFailure)
    {
        var method = typeof(WorkflowEditor).GetMethod("SaveChangesAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        return (Task)method.Invoke(editor, new object?[] { false, false, false, onSuccess, onFailure, null })!;
    }

    private static Task InvokeSaveWithLoaderAsync(WorkflowEditor editor)
    {
        var method = typeof(WorkflowEditor).GetMethod("SaveChangesAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        return (Task)method.Invoke(editor, new object?[] { false, true, false, null, null, null })!;
    }

    private static Task InvokeOnParametersSetAsync(WorkflowEditor editor)
    {
        var method = typeof(WorkflowEditor).GetMethod("OnParametersSetAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        return (Task)method.Invoke(editor, null)!;
    }

    private static void SetWorkflowDefinition(WorkflowEditor editor, WorkflowDefinition definition)
    {
        typeof(WorkflowEditor).GetProperty(nameof(WorkflowEditor.WorkflowDefinition))!.SetValue(editor, definition);
    }

    private static bool GetIsProgressing(WorkflowEditor editor)
    {
        var property = typeof(WorkflowEditorComponentBase).GetProperty("IsProgressing", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        return (bool)property.GetValue(editor)!;
    }

    private static Task InvokeRetractAsync(WorkflowEditor editor, Func<Task>? onSuccess, Func<ValidationErrors, Task>? onFailure)
    {
        var method = typeof(WorkflowEditor).GetMethod("RetractAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        return (Task)method.Invoke(editor, new object?[] { onSuccess, onFailure })!;
    }

    private static WorkflowDefinition CreateDefinition(string name) => new()
    {
        Id = "version-1",
        DefinitionId = "definition-1",
        Name = name,
        Description = "description",
        Root = new JsonObject
        {
            ["id"] = $"{name}-root",
            ["nodeId"] = $"{name}-root",
            ["typeName"] = "Elsa.Workflow",
            ["version"] = 1
        }
    };

    private sealed class DelayedWorkflowDefinitionEditorService : IWorkflowDefinitionEditorService
    {
        public TaskCompletionSource<PendingSave> SaveStarted { get; } = NewCompletionSource<PendingSave>();
        public TaskCompletionSource<PendingSave> SecondSaveStarted { get; } = NewCompletionSource<PendingSave>();
        public TaskCompletionSource<PendingRetract> RetractStarted { get; } = NewCompletionSource<PendingRetract>();

        public Task<Result<SaveWorkflowDefinitionResponse, ValidationErrors>> SaveAsync(WorkflowDefinition workflowDefinition, bool publish, Func<WorkflowDefinition, Task>? workflowSavedCallback = null, CancellationToken cancellationToken = default)
        {
            var pending = new PendingSave(workflowSavedCallback);
            if (!SaveStarted.TrySetResult(pending))
                SecondSaveStarted.TrySetResult(pending);
            return pending.Completion.Task;
        }

        public Task<SaveWorkflowDefinitionResponse> PublishAsync(WorkflowDefinition workflowDefinition, Func<WorkflowDefinition, Task>? workflowPublishedCallback = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Result<WorkflowDefinition, ValidationErrors>> RetractAsync(WorkflowDefinition workflowDefinition, Func<WorkflowDefinition, Task>? workflowRetractedCallback = null, CancellationToken cancellationToken = default)
        {
            var pending = new PendingRetract(workflowRetractedCallback);
            RetractStarted.TrySetResult(pending);
            return pending.Completion.Task;
        }

        public Task<FileDownload> ExportAsync(WorkflowDefinition workflowDefinition, bool includeConsumingWorkflows = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class PendingSave(Func<WorkflowDefinition, Task>? callback)
    {
        public TaskCompletionSource<Result<SaveWorkflowDefinitionResponse, ValidationErrors>> Completion { get; } = NewCompletionSource<Result<SaveWorkflowDefinitionResponse, ValidationErrors>>();
        public bool CallbackInvoked { get; private set; }

        public async Task CompleteSuccessAsync(WorkflowDefinition definition)
        {
            if (callback != null)
            {
                CallbackInvoked = true;
                await callback(definition);
            }

            Completion.TrySetResult(new(new SaveWorkflowDefinitionResponse(definition, false, 0)));
        }

        public void CompleteFailure() => Completion.TrySetResult(new(new ValidationErrors([new("save failed")])));
    }

    private sealed class PendingRetract(Func<WorkflowDefinition, Task>? callback)
    {
        public TaskCompletionSource<Result<WorkflowDefinition, ValidationErrors>> Completion { get; } = NewCompletionSource<Result<WorkflowDefinition, ValidationErrors>>();
        public bool CallbackInvoked { get; private set; }

        public async Task CompleteSuccessAsync(WorkflowDefinition definition)
        {
            if (callback != null)
            {
                CallbackInvoked = true;
                await callback(definition);
            }

            Completion.TrySetResult(new(definition));
        }

        public void CompleteFailure() => Completion.TrySetResult(new(new ValidationErrors([new("retract failed")])));
    }

    private sealed class NoOpActivityRegistry : IActivityRegistry
    {
        public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureLoadedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public IEnumerable<ActivityDescriptor> List() => [];
        public ActivityDescriptor? Find(string activityType, int? version = null) => null;
        public IEnumerable<ActivityDescriptor> FindAll(string activityType) => [];
        public void MarkStale()
        {
        }
    }

    private sealed class NoOpDomAccessor : IDomAccessor
    {
        public Task<DomRect> GetBoundingClientRectAsync(ElementRef elementRef, CancellationToken cancellationToken = default) => Task.FromResult(new DomRect());
        public Task<double> GetVisibleHeightAsync(ElementRef elementRef, CancellationToken cancellationToken = default) => Task.FromResult(0d);
        public Task ClickElementAsync(ElementRef elementRef, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RecordingUserMessageService : IUserMessageService
    {
        public int MessageCount { get; private set; }
        public void ShowSnackbarTextMessage(string message, Severity severity = Severity.Normal, Action<SnackbarOptions>? snackbarOptions = null) => MessageCount++;
        public void ShowSnackbarTextMessage(IEnumerable<string> messages, Severity severity = Severity.Normal, Action<SnackbarOptions>? snackbarOptions = null) => MessageCount++;
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }

    private sealed class TestDiagramDesignerWrapper : DiagramDesignerWrapper
    {
        protected override Task OnInitializedAsync() => Task.CompletedTask;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
        }
    }

    private sealed class TestActivityPropertiesPanel : ActivityPropertiesPanel
    {
        protected override Task OnInitializedAsync() => Task.CompletedTask;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
        }
    }

    private sealed class DelayedActivityVisitor : IActivityVisitor
    {
        private readonly Queue<PendingVisit> _pending = new();

        public PendingVisit Enqueue()
        {
            var pending = new PendingVisit();
            _pending.Enqueue(pending);
            return pending;
        }

        public Task<ActivityNode> VisitAsync(JsonObject activity, CancellationToken cancellationToken = default)
        {
            if (!_pending.TryDequeue(out var pending))
                return Task.FromResult(new ActivityNode(activity));

            pending.Started.TrySetResult(activity);
            return pending.Completion.Task;
        }
    }

    private sealed class PendingVisit
    {
        public TaskCompletionSource<JsonObject> Started { get; } = NewCompletionSource<JsonObject>();
        public TaskCompletionSource<ActivityNode> Completion { get; } = NewCompletionSource<ActivityNode>();

        public void Complete() => Completion.TrySetResult(new ActivityNode(Started.Task.Result));

        public void Fail(Exception exception) => Completion.TrySetException(exception);
    }

    private sealed class TestDiagramDesignerService : IDiagramDesignerService
    {
        public bool HasDiagramDesigner(JsonObject activity) => true;
        public IDiagramDesigner GetDiagramDesigner(JsonObject activity) => new TestDiagramDesigner();
    }

    private sealed class TestDiagramDesigner : IDiagramDesigner
    {
        public Task LoadRootActivityAsync(JsonObject activity, IDictionary<string, ActivityStats>? activityStatsMap) => Task.CompletedTask;
        public Task UpdateActivityAsync(string id, JsonObject activity) => Task.CompletedTask;
        public Task UpdateActivityStatsAsync(string id, ActivityStats stats) => Task.CompletedTask;
        public Task SelectActivityAsync(string id) => Task.CompletedTask;
        public Task<JsonObject> ReadRootActivityAsync() => Task.FromResult(new JsonObject());
        public RenderFragment DisplayDesigner(DisplayContext context) => _ => { };
    }

    private static TaskCompletionSource<T> NewCompletionSource<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
