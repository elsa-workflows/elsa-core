using System.Diagnostics.CodeAnalysis;
using Bunit;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Authorization;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.DomInterop.Models;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Elsa.Studio.Testing;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Extensions;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.Shared.Components;
using Elsa.Studio.Workflows.Tests.Support;
using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.UI.Contexts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public sealed class WorkflowEditorLifecycleTests : BunitContext, IAsyncLifetime
{
    private readonly DelayedWorkflowDefinitionEditorService _editorService = new();
    private readonly RecordingUserMessageService _userMessageService = new();
    private readonly MutableBackendApiClientProvider _backendProvider = new("context-A");
    private IRenderedComponent<MudPopoverProvider> _popovers = null!;

    public WorkflowEditorLifecycleTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddLogging();
        Services.AddCoreInternal();
        Services.AddRemoteBackend();
        Services.AddWorkflowsModule();
        Services.AddSingleton<IBackendApiClientProvider>(_backendProvider);
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<IWorkflowDefinitionEditorService>(_editorService);
        Services.AddSingleton<IActivityRegistry>(NativeCompensationDraftTestData.CreateActivityRegistry());
        Services.AddSingleton<IDomAccessor, NoOpDomAccessor>();
        Services.AddSingleton(_userMessageService);
        Services.AddSingleton<IUserMessageService>(_userMessageService);
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Theory]
    [InlineData(new[] { "workflows/definitions:view" }, false, false)]
    [InlineData(new[] { "workflows/definitions:view", "workflows/definitions:publish" }, true, false)]
    [InlineData(new[] { "workflows/definitions:view", "workflows/definitions:execute" }, false, true)]
    public void Toolbar_OffersPublishAndRunOnlyWithTheirPermissions(string[] grants, bool canPublish, bool canRun)
    {
        var cut = RenderEditor(CreateDefinition("gated"), () => Task.CompletedTask, StubPermissionService.Grants(grants));

        var actions = cut.FindComponents<MudIconButton>().Select(x => x.Instance.Icon).ToList();
        Assert.Equal(canPublish, actions.Contains(Icons.Material.Filled.CloudUpload));
        Assert.Equal(canRun, actions.Contains(Icons.Material.Filled.PlayArrow));
    }

    [Fact]
    public void Toolbar_PublishButtonHasAnAccessibleNameWhenAuthorized()
    {
        var cut = RenderEditor(CreateDefinition("publish"), () => Task.CompletedTask,
            StubPermissionService.Grants(["workflows/definitions:view", "workflows/definitions:publish"]));

        Assert.Single(cut.FindAll("button[aria-label='Publish workflow']"));
    }

    [Fact]
    public async Task SaveUsesOrdinaryServiceForScopeLikeInputAndMetadataOnAnOrdinaryActivity()
    {
        var definition = CreateDefinition("ordinary-data");
        definition.Root = NativeCompensationDraftTestData.CreateOrdinarySetVariableWithScopeLikeUserData();
        Assert.True(NativeCompensationDraftDocumentContext.TryBind(definition, _backendProvider.ContextKey));
        var cut = RenderEditor(definition, () => Task.CompletedTask);

        var saveTask = InvokeSaveAsync(cut.Instance, null, null);
        var pendingSave = await _editorService.SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(1, _editorService.OrdinarySaveCalls);
        Assert.Equal(0, _editorService.NativeDraftSaveCalls);
        pendingSave.CompleteFailure();
        await saveTask.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(new[] { "workflows/definitions:view" }, true, false)]
    [InlineData(new[] { "workflows/definitions:view", "workflows/definitions:retract" }, true, true)]
    [InlineData(new[] { "workflows/definitions:view", "workflows/definitions:retract" }, false, false)]
    public void ToolbarMenu_OffersUnpublishOnlyForAPublishedWorkflowWithTheRetractPermission(string[] grants, bool isPublished, bool canUnpublish)
    {
        var definition = CreateDefinition("gated");
        definition.IsPublished = isPublished;
        var cut = RenderEditor(definition, () => Task.CompletedTask, StubPermissionService.Grants(grants));

        var items = _popovers.OpenMenu(cut.Find(".mud-menu button")).Select(x => x.TextContent.Trim()).ToList();
        Assert.Contains("Save As", items);
        Assert.Equal(canUnpublish, items.Contains("Unpublish"));
    }

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
    public async Task NativeCompensationSaveAndPublishRefusalShowsFocusedSafeSummaryAndClearsOnGraphEdit()
    {
        var definition = CreateDefinition("draft-only");
        definition.Root = JsonNode.Parse("""{"id":"scope-17","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}""")!.AsObject();
        var backend = Services.GetRequiredService<IBackendApiClientProvider>();
        Assert.True(NativeCompensationDraftDocumentContext.TryBind(definition, backend.ContextKey));
        var cut = RenderEditor(definition, () => Task.CompletedTask);
        var failureCallbackCount = 0;

        var saveTask = InvokeSaveAsync(cut.Instance, null, _ =>
        {
            failureCallbackCount++;
            return Task.CompletedTask;
        }, publish: true);
        var pendingSave = await _editorService.SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var validationErrors = ValidationApiExceptionExtensions.GetValidationErrorsFromContent(NativeCompensationRefusalResponse)!;
        pendingSave.CompleteFailure(validationErrors);
        await saveTask.WaitAsync(TimeSpan.FromSeconds(5));

        var summary = cut.WaitForElement("[aria-label='Native compensation publication refusal'][role='alert'][tabindex='-1']");
        Assert.Contains("Native compensation drafts cannot be published.", summary.TextContent);
        Assert.Contains("definition-1", summary.TextContent);
        Assert.Contains("scope-17", summary.TextContent);
        Assert.Contains("step-9", summary.TextContent);
        Assert.Contains("forward-activity-3", summary.TextContent);
        Assert.DoesNotContain("server-private-message", summary.TextContent);
        Assert.Contains(JSInterop.Invocations, invocation => invocation.Identifier == "Blazor._internal.domWrapper.focus");
        Assert.Equal(0, failureCallbackCount);
        Assert.Equal(0, _userMessageService.MessageCount);
        Assert.True(_editorService.LastNativeDraftPublish);
        Assert.Equal(backend.ContextKey, _editorService.LastNativeDraftBackendContextKey);

        definition.Root["changed"] = true;
        await cut.InvokeAsync(() => InvokeGraphUpdatedAsync(cut.Instance));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[aria-label='Native compensation publication refusal']")));
    }

    [Fact]
    public async Task NativeCompensationSaveAndPublishRefusalWithoutReliableContextShowsNoIdentifiers()
    {
        _backendProvider.SupportsAuthenticatedRequestContextBinding = false;
        var definition = CreateCompensationDraftDefinition("unbound-context");
        Assert.True(NativeCompensationDraftDocumentContext.TryBind(definition, _backendProvider.ContextKey));
        var cut = RenderEditor(definition, () => Task.CompletedTask);
        var saveTask = InvokeSaveAsync(cut.Instance, null, null, publish: true);
        var pendingSave = await _editorService.SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        pendingSave.CompleteFailure(ValidationApiExceptionExtensions.GetValidationErrorsFromContent(NativeCompensationRefusalResponse)!);
        await saveTask.WaitAsync(TimeSpan.FromSeconds(5));

        var summary = cut.WaitForElement("[aria-label='Native compensation publication refusal'][role='alert'][tabindex='-1']");
        Assert.Contains("Native compensation drafts cannot be published.", summary.TextContent);
        Assert.DoesNotContain("definition-1", summary.TextContent);
        Assert.DoesNotContain("scope-17", summary.TextContent);
        Assert.DoesNotContain("step-9", summary.TextContent);
        Assert.DoesNotContain("forward-activity-3", summary.TextContent);
        Assert.DoesNotContain("server-private-message", summary.TextContent);
    }

    [Fact]
    public async Task NativeCompensationSaveAndPublishRefusalCompletedAfterBackendSwitchIsDiscarded()
    {
        var definition = CreateCompensationDraftDefinition("context-switch");
        Assert.True(NativeCompensationDraftDocumentContext.TryBind(definition, _backendProvider.ContextKey));
        var cut = RenderEditor(definition, () => Task.CompletedTask);
        var saveTask = InvokeSaveAsync(cut.Instance, null, null, publish: true);
        var pendingSave = await _editorService.SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        _backendProvider.ContextKey = "context-B";
        pendingSave.CompleteFailure(ValidationApiExceptionExtensions.GetValidationErrorsFromContent(NativeCompensationRefusalResponse)!);
        await saveTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Empty(cut.FindAll("[aria-label='Native compensation publication refusal']"));
        Assert.Equal(0, _userMessageService.MessageCount);
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

    // The shell's page guard cascades the user's permissions; without them every action is offered.
    private IRenderedComponent<WorkflowEditor> RenderEditor(WorkflowDefinition definition, Func<Task> workflowDefinitionUpdated, UserPermissions? permissions = null)
    {
        ComponentFactories.Add<DiagramDesignerWrapper, TestDiagramDesignerWrapper>();
        ComponentFactories.Add<ActivityPropertiesPanel, TestActivityPropertiesPanel>();
        _popovers = Render<MudPopoverProvider>();
        return Render<WorkflowEditor>(parameters => parameters
            .Add(x => x.WorkflowDefinition, definition)
            .Add(x => x.WorkflowDefinitionUpdated, workflowDefinitionUpdated)
            .AddCascadingValue(permissions ?? UserPermissions.Unknown));
    }

    private static Task InvokeSaveAsync(WorkflowEditor editor, Func<SaveWorkflowDefinitionResponse, Task>? onSuccess, Func<ValidationErrors, Task>? onFailure, bool publish = false)
    {
        var method = typeof(WorkflowEditor).GetMethod("SaveChangesAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        return (Task)method.Invoke(editor, new object?[] { false, false, publish, onSuccess, onFailure, null })!;
    }

    private static Task InvokeGraphUpdatedAsync(WorkflowEditor editor)
    {
        var method = typeof(WorkflowEditor).GetMethod("OnGraphUpdated", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        return (Task)method.Invoke(editor, null)!;
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

    private static WorkflowDefinition CreateCompensationDraftDefinition(string name)
    {
        var definition = CreateDefinition(name);
        definition.Root = JsonNode.Parse("""{"id":"scope-17","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}""")!.AsObject();
        return definition;
    }

    private const string NativeCompensationRefusalResponse = """
    {
      "errors": { "generalErrors": ["server-private-message"] },
      "code": "NATIVE_COMPENSATION_DRAFT_ONLY",
      "data": { "diagnostics": [{
        "scopeActivityId": "scope-17",
        "stepId": "step-9",
        "role": "forward",
        "relatedActivityIds": ["forward-activity-3"],
        "mappingIds": ["capture-2"],
        "mappingId": "capture-2"
      }] }
    }
    """;

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

    private sealed class MutableBackendApiClientProvider(string contextKey) : IBackendApiClientProvider, IBackendRequestContextBinding
    {
        public Uri Url => new("http://localhost");
        public string ContextKey { get; set; } = contextKey;
        public bool SupportsAuthenticatedRequestContextBinding { get; set; } = true;
        public bool HasActiveRequestContextBinding => true;

        public void RegisterAuthenticatedRequestTransport(IBackendRequestContextEpochSource? epochSource)
        {
        }

        public IDisposable BindRequestContext(string expectedContextKey)
        {
            if (!string.Equals(ContextKey, expectedContextKey, StringComparison.Ordinal))
                throw new BackendRequestContextChangedException();

            return new NoopDisposable();
        }

        public void ValidateBoundRequestContext(IBackendRequestContextEpochSource? requestEpochSource)
        {
        }

        public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class => throw new NotSupportedException();

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    private sealed class DelayedWorkflowDefinitionEditorService : IWorkflowDefinitionEditorService, IContextBoundWorkflowDefinitionEditorService
    {
        public bool LastNativeDraftPublish { get; private set; }
        public string? LastNativeDraftBackendContextKey { get; private set; }
        public int OrdinarySaveCalls { get; private set; }
        public int NativeDraftSaveCalls { get; private set; }

        public TaskCompletionSource<PendingSave> SaveStarted { get; } = NewCompletionSource<PendingSave>();
        public TaskCompletionSource<PendingSave> SecondSaveStarted { get; } = NewCompletionSource<PendingSave>();
        public TaskCompletionSource<PendingRetract> RetractStarted { get; } = NewCompletionSource<PendingRetract>();

        public Task<Result<SaveWorkflowDefinitionResponse, ValidationErrors>> SaveAsync(WorkflowDefinition workflowDefinition, bool publish, Func<WorkflowDefinition, Task>? workflowSavedCallback = null, CancellationToken cancellationToken = default)
        {
            OrdinarySaveCalls++;
            return StartSave(workflowSavedCallback);
        }

        private Task<Result<SaveWorkflowDefinitionResponse, ValidationErrors>> StartSave(Func<WorkflowDefinition, Task>? workflowSavedCallback)
        {
            var pending = new PendingSave(workflowSavedCallback);
            if (!SaveStarted.TrySetResult(pending))
                SecondSaveStarted.TrySetResult(pending);
            return pending.Completion.Task;
        }

        public Task<Result<SaveWorkflowDefinitionResponse, ValidationErrors>> SaveNativeCompensationDraftAsync(WorkflowDefinition workflowDefinition, bool publish, string expectedBackendContextKey, Func<WorkflowDefinition, Task>? workflowSavedCallback = null, CancellationToken cancellationToken = default)
        {
            NativeDraftSaveCalls++;
            LastNativeDraftPublish = publish;
            LastNativeDraftBackendContextKey = expectedBackendContextKey;
            return StartSave(workflowSavedCallback);
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

        public void CompleteFailure() => CompleteFailure(new ValidationErrors([new("save failed")]));

        public void CompleteFailure(ValidationErrors errors) => Completion.TrySetResult(new(errors));
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
