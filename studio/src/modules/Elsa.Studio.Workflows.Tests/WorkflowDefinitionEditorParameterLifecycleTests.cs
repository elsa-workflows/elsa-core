using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bunit;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Studio.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Elsa.Studio.Extensions;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

using MetadataComponent = Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Properties.Sections.Metadata.Metadata;
using Elsa.Studio.Testing;

namespace Elsa.Studio.Workflows.Tests;

public sealed class WorkflowDefinitionEditorParameterLifecycleTests
{
    [Fact]
    public async Task WorkspacePreservesInternalSelectionWhenIncomingParametersAreUnchanged()
    {
        await using var context = CreateContext();
        var initialDefinition = CreateDefinition("initial", "version-1");
        var selectedDefinition = CreateDefinition("selected", "version-0");
        var cut = RenderWorkspace(context, initialDefinition, initialDefinition);

        await cut.InvokeAsync(() => cut.Instance.DisplayWorkflowDefinitionVersionAsync(selectedDefinition));
        cut.Render(parameters => parameters
            .Add(x => x.WorkflowDefinition, initialDefinition)
            .Add(x => x.SelectedWorkflowDefinition, initialDefinition));

        Assert.Same(selectedDefinition, cut.Instance.GetSelectedDefinition());
    }

    [Fact]
    public async Task WorkspacePreflightsCapabilityForAnOrdinaryOriginBoundDefinitionBeforeItsFirstNativeScope()
    {
        await using var context = CreateContext();
        var backend = new MutableBackendApiClientProvider("context-A");
        var featureProvider = new EnabledRemoteFeatureProvider();
        context.Services.AddSingleton<IBackendApiClientProvider>(backend);
        context.Services.AddSingleton<IRemoteFeatureProvider>(featureProvider);
        var definition = CreateDefinition("ordinary definition", "version-a");
        definition.Root = System.Text.Json.Nodes.JsonNode.Parse("""{"id":"write-1","nodeId":"write-1","type":"Elsa.WriteLine","version":1}""")!.AsObject();
        Assert.True(NativeCompensationDraftDocumentContext.TryBind(definition, "context-A"));

        var cut = RenderWorkspace(context, definition, definition);

        Assert.Equal(1, featureProvider.FreshCheckCount);
        Assert.False(cut.Instance.IsReadOnly);

        definition.Root = CreateNativeCompensationScope();
        cut.Render(parameters => parameters
            .Add(x => x.WorkflowDefinition, definition)
            .Add(x => x.SelectedWorkflowDefinition, definition));

        Assert.False(cut.Instance.IsReadOnly);
        Assert.Single(cut.FindComponents<WorkflowEditor>());
        Assert.Equal(1, featureProvider.FreshCheckCount);
    }

    [Fact]
    public async Task WorkspaceRetriesACompletedNegativeCapabilityCheckForTheSameOrigin()
    {
        await using var context = CreateContext();
        var backend = new MutableBackendApiClientProvider("context-A");
        var featureProvider = new EnabledRemoteFeatureProvider(false);
        context.Services.AddSingleton<IBackendApiClientProvider>(backend);
        context.Services.AddSingleton<IRemoteFeatureProvider>(featureProvider);
        var definition = CreateDefinition("ordinary definition", "version-a");
        definition.Root = System.Text.Json.Nodes.JsonNode.Parse("""{"id":"write-1","nodeId":"write-1","type":"Elsa.WriteLine","version":1}""")!.AsObject();
        Assert.True(NativeCompensationDraftDocumentContext.TryBind(definition, "context-A"));
        var cut = RenderWorkspace(context, definition, definition);

        Assert.Equal(1, featureProvider.FreshCheckCount);
        definition.Root = CreateNativeCompensationScope();
        featureProvider.Enabled = true;
        cut.Render(parameters => parameters
            .Add(x => x.WorkflowDefinition, definition)
            .Add(x => x.SelectedWorkflowDefinition, definition));

        cut.WaitForAssertion(() =>
        {
            Assert.False(cut.Instance.IsReadOnly);
            Assert.Single(cut.FindComponents<WorkflowEditor>());
        });
        Assert.Equal(2, featureProvider.FreshCheckCount);
    }

    [Fact]
    public async Task WorkspaceAdoptsValidOriginFromAnInternalNativeSaveResponseBeforeRendering()
    {
        await using var context = CreateContext();
        var backend = new MutableBackendApiClientProvider("context-A");
        var featureProvider = new EnabledRemoteFeatureProvider();
        context.Services.AddSingleton<IBackendApiClientProvider>(backend);
        context.Services.AddSingleton<IRemoteFeatureProvider>(featureProvider);
        var definition = CreateDefinition("ordinary definition", "version-a");
        definition.Root = System.Text.Json.Nodes.JsonNode.Parse("""{"id":"write-1","nodeId":"write-1","type":"Elsa.WriteLine","version":1}""")!.AsObject();
        Assert.True(NativeCompensationDraftDocumentContext.TryBind(definition, "context-A"));
        var cut = RenderWorkspace(context, definition, definition);

        definition.Root = CreateNativeCompensationScope();
        Assert.False(cut.Instance.IsReadOnly);
        var editor = cut.FindComponent<WorkflowEditor>().Instance;
        var saveTask = InvokeSaveAsync(editor);
        var pendingSave = await context.Services.GetRequiredService<DelayedWorkflowDefinitionEditorService>().SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var savedDefinition = CreateDefinition("saved native definition", "version-b");
        savedDefinition.Root = CreateNativeCompensationScope();
        Assert.True(NativeCompensationDraftDocumentContext.TryBind(savedDefinition, "context-A"));

        await pendingSave.CompleteSuccessAsync(savedDefinition);
        await saveTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Same(savedDefinition, cut.Instance.GetSelectedDefinition());
        Assert.Equal("context-A", NativeCompensationDraftDocumentContext.GetBackendContextKey(savedDefinition));
        Assert.False(cut.Instance.IsReadOnly);
        Assert.Single(cut.FindComponents<WorkflowEditor>());
        Assert.Equal(2, featureProvider.FreshCheckCount);
    }

    [Fact]
    public async Task RepeatedCapabilityRefreshDoesNotCancelTheFreshCheckForAnAdoptedSaveResponse()
    {
        await using var context = CreateContext();
        var backend = new MutableBackendApiClientProvider("context-A");
        var featureProvider = new EnabledRemoteFeatureProvider();
        context.Services.AddSingleton<IBackendApiClientProvider>(backend);
        context.Services.AddSingleton<IRemoteFeatureProvider>(featureProvider);
        var definition = CreateDefinition("ordinary definition", "version-a");
        definition.Root = System.Text.Json.Nodes.JsonNode.Parse("""{"id":"write-1","nodeId":"write-1","type":"Elsa.WriteLine","version":1}""")!.AsObject();
        Assert.True(NativeCompensationDraftDocumentContext.TryBind(definition, "context-A"));
        var cut = RenderWorkspace(context, definition, definition);

        definition.Root = CreateNativeCompensationScope();
        var editor = cut.FindComponent<WorkflowEditor>().Instance;
        var saveTask = InvokeSaveAsync(editor);
        var pendingSave = await context.Services.GetRequiredService<DelayedWorkflowDefinitionEditorService>().SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var savedDefinition = CreateDefinition("saved native definition", "version-b");
        savedDefinition.Root = CreateNativeCompensationScope();
        Assert.True(NativeCompensationDraftDocumentContext.TryBind(savedDefinition, "context-A"));
        var capabilityResponse = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        featureProvider.FreshCheck = () => capabilityResponse.Task;

        var saveResponseTask = pendingSave.CompleteSuccessAsync(savedDefinition);
        await featureProvider.DeferredCheckStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await cut.InvokeAsync(() => InvokeCapabilityRefreshAsync(cut.Instance));
        capabilityResponse.SetResult(false);
        await saveResponseTask.WaitAsync(TimeSpan.FromSeconds(5));
        await saveTask.WaitAsync(TimeSpan.FromSeconds(5));

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Instance.IsReadOnly);
            Assert.Empty(cut.FindComponents<WorkflowEditor>());
            Assert.Single(cut.FindComponents<WorkflowDefinitionVersionViewer>());
            Assert.Same(savedDefinition, cut.Instance.GetSelectedDefinition());
        });
        Assert.Equal(2, featureProvider.FreshCheckCount);
    }

    [Fact]
    public async Task WorkspaceHidesNativeDraftWhenAnInternalReplacementHasAStaleOrigin()
    {
        await using var context = CreateContext();
        var backend = new MutableBackendApiClientProvider("context-A");
        var featureProvider = new EnabledRemoteFeatureProvider();
        context.Services.AddSingleton<IBackendApiClientProvider>(backend);
        context.Services.AddSingleton<IRemoteFeatureProvider>(featureProvider);
        var definition = CreateDefinition("ordinary definition", "version-a");
        definition.Root = System.Text.Json.Nodes.JsonNode.Parse("""{"id":"write-1","nodeId":"write-1","type":"Elsa.WriteLine","version":1}""")!.AsObject();
        Assert.True(NativeCompensationDraftDocumentContext.TryBind(definition, "context-A"));
        var cut = RenderWorkspace(context, definition, definition);

        definition.Root = CreateNativeCompensationScope();
        var editor = cut.FindComponent<WorkflowEditor>().Instance;
        var saveTask = InvokeSaveAsync(editor);
        var pendingSave = await context.Services.GetRequiredService<DelayedWorkflowDefinitionEditorService>().SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var staleDefinition = CreateDefinition("stale native definition", "version-b");
        staleDefinition.Root = CreateNativeCompensationScope();
        Assert.True(NativeCompensationDraftDocumentContext.TryBind(staleDefinition, "context-B"));

        await pendingSave.CompleteSuccessAsync(staleDefinition);
        await saveTask.WaitAsync(TimeSpan.FromSeconds(5));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("This compensation draft was loaded from a different backend context", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(cut.FindComponents<WorkflowEditor>());
            Assert.Same(staleDefinition, cut.Instance.GetSelectedDefinition());
            Assert.Equal("context-B", NativeCompensationDraftDocumentContext.GetBackendContextKey(staleDefinition));
        });
    }

    [Fact]
    public async Task WorkspaceKeepsFirstNativeScopeReadOnlyWhenFreshCapabilityIsUnavailable()
    {
        await using var context = CreateContext();
        var backend = new MutableBackendApiClientProvider("context-A");
        var featureProvider = new EnabledRemoteFeatureProvider(false);
        context.Services.AddSingleton<IBackendApiClientProvider>(backend);
        context.Services.AddSingleton<IRemoteFeatureProvider>(featureProvider);
        var definition = CreateDefinition("ordinary definition", "version-a");
        definition.Root = System.Text.Json.Nodes.JsonNode.Parse("""{"id":"write-1","type":"Elsa.WriteLine","version":1}""")!.AsObject();
        Assert.True(NativeCompensationDraftDocumentContext.TryBind(definition, "context-A"));
        var cut = RenderWorkspace(context, definition, definition);

        definition.Root = CreateNativeCompensationScope();
        cut.Render(parameters => parameters
            .Add(x => x.WorkflowDefinition, definition)
            .Add(x => x.SelectedWorkflowDefinition, definition));

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Instance.IsReadOnly);
            Assert.Empty(cut.FindComponents<WorkflowEditor>());
            Assert.Single(cut.FindComponents<WorkflowDefinitionVersionViewer>());
        });
    }

    [Fact]
    public async Task WorkspaceHidesAnOriginBoundCompensationDraftAfterContextSwitchUntilExplicitReload()
    {
        await using var context = CreateContext();
        var backend = new MutableBackendApiClientProvider("context-A");
        context.Services.AddSingleton<IBackendApiClientProvider>(backend);
        context.Services.AddSingleton<IRemoteFeatureProvider>(new EnabledRemoteFeatureProvider());
        var definitionService = (WorkflowDefinitionServiceProxy)context.Services.GetServices<IWorkflowDefinitionService>().Last();
        var originalDefinition = CreateDefinition("origin A draft", "version-a");
        originalDefinition.Root = System.Text.Json.Nodes.JsonNode.Parse("""{"id":"scope-a","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}""")!.AsObject();
        Assert.True(NativeCompensationDraftDocumentContext.TryBind(originalDefinition, "context-A"));
        var cut = RenderWorkspace(context, originalDefinition, originalDefinition);

        Assert.False(cut.Instance.IsReadOnly);
        Assert.Single(cut.FindComponents<WorkflowEditor>());
        backend.ContextKey = "context-B";
        context.Services.GetRequiredService<IPermissionRefreshSignal>().Raise();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("This compensation draft was loaded from a different backend context", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(cut.FindComponents<WorkflowEditor>());
            Assert.Empty(cut.FindComponents<CodeView>());
            Assert.Empty(cut.FindComponents<WorkflowProperties>());
            Assert.Same(originalDefinition, cut.Instance.GetSelectedDefinition());
            Assert.Equal("context-A", NativeCompensationDraftDocumentContext.GetBackendContextKey(originalDefinition));
        });

        var reloadedDefinition = CreateDefinition("current B draft", "version-b");
        reloadedDefinition.Root = System.Text.Json.Nodes.JsonNode.Parse("""{"id":"scope-b","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}""")!.AsObject();
        definitionService.FindResultFactory = _ =>
        {
            NativeCompensationDraftDocumentContext.TryBind(reloadedDefinition, backend.ContextKey);
            return reloadedDefinition;
        };
        var reloadButton = cut.FindAll("button").Single(button => button.TextContent.Contains("Reload from current backend", StringComparison.OrdinalIgnoreCase));
        await reloadButton.ClickAsync();

        cut.WaitForAssertion(() =>
        {
            Assert.Same(reloadedDefinition, cut.Instance.GetSelectedDefinition());
            Assert.Equal("context-B", NativeCompensationDraftDocumentContext.GetBackendContextKey(reloadedDefinition));
            Assert.Single(cut.FindComponents<WorkflowEditor>());
        });
        Assert.Equal("context-A", NativeCompensationDraftDocumentContext.GetBackendContextKey(originalDefinition));
    }

    [Fact]
    public async Task WorkspaceHidesNativeDraftImmediatelyAndIgnoresLateCapabilityAfterContextSwitch()
    {
        await using var context = CreateContext();
        var backend = new MutableBackendApiClientProvider("context-A");
        var featureProvider = new DeferredFreshFeatureProvider();
        context.Services.AddSingleton<IBackendApiClientProvider>(backend);
        context.Services.AddSingleton<IRemoteFeatureProvider>(featureProvider);
        var definition = CreateDefinition("origin A draft", "version-a");
        definition.Root = System.Text.Json.Nodes.JsonNode.Parse("""{"id":"scope-a","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}""")!.AsObject();
        Assert.True(NativeCompensationDraftDocumentContext.TryBind(definition, "context-A"));

        var cut = RenderWorkspace(context, definition, definition);
        await featureProvider.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Single(cut.FindComponents<WorkflowDefinitionVersionViewer>());

        backend.ContextKey = "context-B";
        context.Services.GetRequiredService<IPermissionRefreshSignal>().Raise();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("local draft is hidden", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(cut.FindComponents<WorkflowEditor>());
            Assert.Empty(cut.FindComponents<CodeView>());
            Assert.Empty(cut.FindComponents<WorkflowProperties>());
            Assert.Empty(cut.FindComponents<WorkflowDefinitionVersionViewer>());
            Assert.Same(definition, cut.Instance.GetSelectedDefinition());
        });

        featureProvider.Complete(true);
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("local draft is hidden", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(cut.FindComponents<WorkflowEditor>());
            Assert.Empty(cut.FindComponents<WorkflowDefinitionVersionViewer>());
            Assert.Equal("context-A", NativeCompensationDraftDocumentContext.GetBackendContextKey(definition));
        });
    }

    [Theory]
    [InlineData("""{"type":"Elsa.NotFoundActivity","originalActivityJson":"{\"type\":\"Elsa.NotFoundActivity\",\"originalActivityJson\":\"{\\\"type\\\":\\\"Elsa.CompensationScope\\\",\\\"version\\\":9}\"}"}""")]
    [InlineData("""{"type":"Elsa.NotFoundActivity","missingTypeName":"Elsa.CompensationScope"}""")]
    [InlineData("""{"type":"Elsa.CompensationScope","version":2,"draftSchemaVersion":1,"steps":[]}""")]
    public async Task WorkspaceKeepsOpaqueAndUnsupportedNativeDraftsReadOnlyEvenWithCapability(string rootJson)
    {
        await using var context = CreateContext();
        var backend = new MutableBackendApiClientProvider("context-A");
        context.Services.AddSingleton<IBackendApiClientProvider>(backend);
        context.Services.AddSingleton<IRemoteFeatureProvider>(new EnabledRemoteFeatureProvider());
        var definition = CreateDefinition("native draft", "version-a");
        definition.Root = System.Text.Json.Nodes.JsonNode.Parse(rootJson)!.AsObject();
        Assert.True(NativeCompensationDraftDocumentContext.TryBind(definition, "context-A"));
        var cut = RenderWorkspace(context, definition, definition);

        Assert.True(cut.Instance.IsReadOnly);
        Assert.Empty(cut.FindComponents<WorkflowEditor>());
        Assert.Single(cut.FindComponents<WorkflowDefinitionVersionViewer>());
        Assert.Contains("This workflow contains native compensation data that Studio cannot safely edit", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("read-only", cut.Markup, StringComparison.OrdinalIgnoreCase);
        cut.FindAll("[role=tab]").Single(tab => tab.TextContent.Contains("Code View", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindComponents<CodeView>()));
        var codeView = cut.FindComponent<CodeView>().Instance;
        Assert.True(codeView.IsReadOnly);
        var metadata = cut.FindComponent<WorkflowProperties>().FindComponent<MetadataComponent>().Instance;
        var metadataReadOnly = (bool)typeof(MetadataComponent)
            .GetProperty("IsReadOnly", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(metadata)!;
        Assert.True(metadataReadOnly);

        var attemptedDefinition = CreateDefinition("mutated by code view", "version-a");
        attemptedDefinition.Root = System.Text.Json.Nodes.JsonNode.Parse("""{"type":"Elsa.WriteLine","version":1}""")!.AsObject();
        var codeViewUpdate = typeof(CodeView).GetMethod("UpdateEditorFromCodeViewAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Assert.False(await (Task<bool>)codeViewUpdate.Invoke(codeView, null)!);
        var applyDefinition = typeof(WorkflowDefinitionWorkspace).GetMethod("OnApplyWorkflowDefinition", BindingFlags.Instance | BindingFlags.NonPublic)!;
        await cut.InvokeAsync(() => (Task)applyDefinition.Invoke(cut.Instance, [attemptedDefinition])!);

        Assert.Same(definition, cut.Instance.GetSelectedDefinition());
        Assert.Equal("native draft", definition.Name);
    }

    [Fact]
    public async Task OrdinaryDefinitionKeepsItsLoadOriginIfItGainsACompensationScopeAfterContextSwitch()
    {
        await using var context = CreateContext();
        var backend = new MutableBackendApiClientProvider("context-A");
        context.Services.AddSingleton<IBackendApiClientProvider>(backend);
        context.Services.AddSingleton<IRemoteFeatureProvider>(new EnabledRemoteFeatureProvider());
        var definition = CreateDefinition("ordinary definition", "version-a");
        definition.Root = System.Text.Json.Nodes.JsonNode.Parse("""{"id":"write-1","type":"Elsa.WriteLine","version":1}""")!.AsObject();
        Assert.True(NativeCompensationDraftDocumentContext.TryBind(definition, "context-A"));
        var cut = RenderWorkspace(context, definition, definition);

        backend.ContextKey = "context-B";
        context.Services.GetRequiredService<IPermissionRefreshSignal>().Raise();
        definition.Root = System.Text.Json.Nodes.JsonNode.Parse("""{"id":"scope-a","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}""")!.AsObject();
        cut.Render(parameters => parameters
            .Add(x => x.WorkflowDefinition, definition)
            .Add(x => x.SelectedWorkflowDefinition, definition));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("This compensation draft was loaded from a different backend context", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(cut.FindComponents<WorkflowEditor>());
            Assert.Same(definition, cut.Instance.GetSelectedDefinition());
            Assert.Equal("context-A", NativeCompensationDraftDocumentContext.GetBackendContextKey(definition));
        });
    }

    [Fact]
    public async Task WorkspaceReloadSignalRehydratesMetadataOnlyForANewIncomingReference()
    {
        await using var context = CreateContext();
        var initialDefinition = CreateDefinition("initial", "version-1");
        var cut = RenderWorkspace(context, initialDefinition, initialDefinition);
        var metadata = cut.FindComponent<MetadataComponent>();
        var fields = metadata.FindComponents<MudTextField<string>>();
        var nameInput = fields[0].Find("input");
        var descriptionInput = fields[1].Find("textarea");

        await nameInput.InputAsync("validated name");
        await nameInput.BlurAsync();
        await descriptionInput.InputAsync("draft description");

        cut.Render(parameters => parameters
            .Add(x => x.WorkflowDefinition, initialDefinition)
            .Add(x => x.SelectedWorkflowDefinition, initialDefinition));

        Assert.Equal("validated name", cut.FindComponent<MetadataComponent>().FindComponents<MudTextField<string>>()[0].Find("input").GetAttribute("value"));
        Assert.Equal("draft description", cut.FindComponent<MetadataComponent>().FindComponents<MudTextField<string>>()[1].Find("textarea").GetAttribute("value"));

        var replacementDefinition = CreateDefinition("replacement name", "version-1");
        replacementDefinition.Description = "replacement description";
        cut.Render(parameters => parameters
            .Add(x => x.WorkflowDefinition, replacementDefinition)
            .Add(x => x.SelectedWorkflowDefinition, replacementDefinition));

        fields = cut.FindComponent<MetadataComponent>().FindComponents<MudTextField<string>>();
        Assert.Equal("replacement name", fields[0].Find("input").GetAttribute("value"));
        Assert.Equal("replacement description", fields[1].Find("textarea").GetAttribute("value"));
        Assert.Equal("replacement name", replacementDefinition.Name);
        Assert.Equal("replacement description", replacementDefinition.Description);
    }

    [Fact]
    public async Task WorkspaceAppliesNewSameVersionIncomingReference()
    {
        await using var context = CreateContext();
        var initialDefinition = CreateDefinition("initial", "version-1");
        var replacementDefinition = CreateDefinition("replacement", "version-1");
        var cut = RenderWorkspace(context, initialDefinition, initialDefinition);
        var editor = cut.FindComponent<WorkflowEditor>().Instance;

        cut.Render(parameters => parameters
            .Add(x => x.WorkflowDefinition, replacementDefinition)
            .Add(x => x.SelectedWorkflowDefinition, replacementDefinition));
        cut.Render(parameters => parameters
            .Add(x => x.WorkflowDefinition, replacementDefinition)
            .Add(x => x.SelectedWorkflowDefinition, replacementDefinition));

        Assert.Same(replacementDefinition, cut.Instance.GetSelectedDefinition());
        Assert.Same(replacementDefinition, editor.WorkflowDefinition);
    }

    [Fact]
    public async Task WorkflowDefinitionEditorDoesNotRefetchWhenDefinitionIdIsUnchanged()
    {
        await using var context = CreateContext();
        var service = AddWorkflowDefinitionServiceProxy(context);
        var cut = RenderOuterEditor(context, "definition-1");
        cut.Render(parameters => parameters.Add(x => x.DefinitionId, "definition-1"));

        Assert.Equal(1, service.FindByDefinitionIdCallCount);

        cut.Render(parameters => parameters.Add(x => x.DefinitionId, "definition-2"));

        Assert.Equal(2, service.FindByDefinitionIdCallCount);
    }

    [Fact]
    public async Task WorkflowDefinitionEditorCoalescesSameDefinitionLoadsWhileTheCurrentLoadIsPending()
    {
        await using var context = CreateContext();
        var service = AddWorkflowDefinitionServiceProxy(context);
        var pendingLoad = service.EnqueueFind();
        var cut = RenderOuterEditor(context, "definition-a");

        cut.Render(parameters => parameters.Add(x => x.DefinitionId, "definition-a"));
        cut.Render(parameters => parameters.Add(x => x.DefinitionId, "definition-a"));

        Assert.Equal(1, service.FindByDefinitionIdCallCount);

        var definition = CreateDefinition("definition a", "version-a");
        definition.DefinitionId = "definition-a";
        pendingLoad.SetResult(definition);

        cut.WaitForAssertion(() => Assert.Same(definition, cut.Instance.GetSelectedWorkflowDefinitionVersion()));
        Assert.Equal(1, service.FindByDefinitionIdCallCount);
    }

    [Fact]
    public async Task WorkflowDefinitionEditorKeepsTheLatestOutOfOrderDefinitionLoad()
    {
        await using var context = CreateContext();
        var service = AddWorkflowDefinitionServiceProxy(context);
        var firstLoad = service.EnqueueFind();
        var cut = RenderOuterEditor(context, "definition-a");
        var secondLoad = service.EnqueueFind();
        cut.Render(parameters => parameters.Add(x => x.DefinitionId, "definition-b"));
        var latestLoad = service.EnqueueFind();
        cut.Render(parameters => parameters.Add(x => x.DefinitionId, "definition-a"));

        var latestDefinition = CreateDefinition("latest definition a", "version-a-latest");
        latestLoad.SetResult(latestDefinition);

        cut.WaitForAssertion(() => Assert.Same(latestDefinition, cut.Instance.GetSelectedWorkflowDefinitionVersion()));

        var renderCount = cut.RenderCount;
        firstLoad.SetResult(CreateDefinition("stale definition a", "version-a-old"));
        cut.WaitForState(() => cut.RenderCount > renderCount, TimeSpan.FromSeconds(5));
        Assert.Same(latestDefinition, cut.Instance.GetSelectedWorkflowDefinitionVersion());

        renderCount = cut.RenderCount;
        secondLoad.SetResult(CreateDefinition("stale definition b", "version-b-old"));
        cut.WaitForState(() => cut.RenderCount > renderCount, TimeSpan.FromSeconds(5));
        Assert.Same(latestDefinition, cut.Instance.GetSelectedWorkflowDefinitionVersion());
        Assert.Equal(3, service.FindByDefinitionIdCallCount);
    }

    [Fact]
    public async Task WorkflowDefinitionEditorIgnoresFailureFromAnObsoleteDefinitionLoad()
    {
        await using var context = CreateContext();
        var service = AddWorkflowDefinitionServiceProxy(context);
        var firstLoad = service.EnqueueFind();
        var cut = RenderOuterEditor(context, "definition-a");
        var secondLoad = service.EnqueueFind();
        cut.Render(parameters => parameters.Add(x => x.DefinitionId, "definition-b"));

        var latestDefinition = CreateDefinition("latest definition b", "version-b-latest");
        latestDefinition.DefinitionId = "definition-b";
        secondLoad.SetResult(latestDefinition);
        cut.WaitForAssertion(() => Assert.Same(latestDefinition, cut.Instance.GetSelectedWorkflowDefinitionVersion()));

        var renderCount = cut.RenderCount;
        firstLoad.SetException(new InvalidOperationException("obsolete definition load failed"));
        cut.WaitForState(() => cut.RenderCount > renderCount, TimeSpan.FromSeconds(5));

        Assert.Same(latestDefinition, cut.Instance.GetSelectedWorkflowDefinitionVersion());
    }

    [Fact]
    public async Task WorkflowDefinitionEditorPropagatesCurrentLoadFailureAndRetriesAfterFailure()
    {
        await using var context = CreateContext();
        var service = AddWorkflowDefinitionServiceProxy(context);
        var cut = RenderOuterEditor(context, "definition-a");
        service.FindException = new InvalidOperationException("current definition load failed");

        var exception = Assert.Throws<InvalidOperationException>(() => cut.Render(parameters => parameters.Add(x => x.DefinitionId, "definition-b")));

        Assert.Equal("current definition load failed", exception.Message);

        service.FindException = null;
        var recoveredDefinition = CreateDefinition("recovered", "version-b");
        recoveredDefinition.DefinitionId = "definition-b";
        service.FindResultFactory = _ => recoveredDefinition;
        cut.Render(parameters => parameters.Add(x => x.DefinitionId, "definition-b"));

        Assert.Same(recoveredDefinition, cut.Instance.GetSelectedWorkflowDefinitionVersion());
        Assert.Equal(3, service.FindByDefinitionIdCallCount);
    }

    [Fact]
    public async Task WorkflowDefinitionEditorClearsAWorkflowWhenLatestLoadReturnsNullAndRetriesIt()
    {
        await using var context = CreateContext();
        var service = AddWorkflowDefinitionServiceProxy(context);
        var loadedDefinition = CreateDefinition("loaded", "version-1");
        service.FindResultFactory = _ => loadedDefinition;
        var cut = RenderOuterEditor(context, "definition-a");

        service.FindResultFactory = _ => null;
        cut.Render(parameters => parameters.Add(x => x.DefinitionId, "definition-b"));
        Assert.Null(cut.Instance.GetSelectedWorkflowDefinitionVersion());

        var recoveredDefinition = CreateDefinition("recovered", "version-b");
        service.FindResultFactory = _ => recoveredDefinition;
        cut.Render(parameters => parameters.Add(x => x.DefinitionId, "definition-b"));

        Assert.Same(recoveredDefinition, cut.Instance.GetSelectedWorkflowDefinitionVersion());
        Assert.Equal(3, service.FindByDefinitionIdCallCount);
    }

    private static BunitContext CreateContext()
    {
        var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddLogging();
        context.Services.AddMudServices();
        context.Services.AddCoreInternal();
        context.Services.AddRemoteBackend();
        context.Services.AddWorkflowsModule();
        context.Services.AddSingleton<ILocalizer, TestLocalizer>();
        var editorService = new DelayedWorkflowDefinitionEditorService();
        context.Services.AddSingleton(editorService);
        context.Services.AddSingleton<IWorkflowDefinitionEditorService>(editorService);
        var workflowDefinitionService = DispatchProxy.Create<IWorkflowDefinitionService, WorkflowDefinitionServiceProxy>();
        context.Services.AddSingleton(workflowDefinitionService);
        context.Services.AddSingleton<IActivityRegistry, NoOpActivityRegistry>();
        context.Services.AddSingleton<IActivityPickerComponentProvider, EmptyActivityPickerComponentProvider>();
        context.ComponentFactories.Add<WorkflowEditor, TestWorkflowEditor>();
        context.ComponentFactories.Add<WorkflowDefinitionVersionViewer, TestWorkflowDefinitionVersionViewer>();
        context.ComponentFactories.Add<CodeView, TestCodeView>();
        context.ComponentFactories.Add<WorkflowProperties, TestWorkflowProperties>();
        return context;
    }

    private static IRenderedComponent<WorkflowDefinitionWorkspace> RenderWorkspace(BunitContext context, WorkflowDefinition workflowDefinition, WorkflowDefinition selectedWorkflowDefinition) =>
        RenderWithPopoverProvider(context, () => context.Render<WorkflowDefinitionWorkspace>(parameters => parameters
            .Add(x => x.WorkflowDefinition, workflowDefinition)
            .Add(x => x.SelectedWorkflowDefinition, selectedWorkflowDefinition)));

    private static IRenderedComponent<WorkflowDefinitionEditor> RenderOuterEditor(BunitContext context, string definitionId) =>
        RenderWithPopoverProvider(context, () => context.Render<WorkflowDefinitionEditor>(parameters => parameters.Add(x => x.DefinitionId, definitionId)));

    private static T RenderWithPopoverProvider<T>(BunitContext context, Func<T> render)
    {
        context.Render<MudPopoverProvider>();
        return render();
    }

    private static WorkflowDefinitionServiceProxy AddWorkflowDefinitionServiceProxy(BunitContext context)
    {
        var service = DispatchProxy.Create<IWorkflowDefinitionService, WorkflowDefinitionServiceProxy>();
        context.Services.AddSingleton(service);
        return (WorkflowDefinitionServiceProxy)(object)service;
    }

    private static WorkflowDefinition CreateDefinition(string name, string versionId) => new()
    {
        Id = versionId,
        DefinitionId = "definition-1",
        Name = name,
        Description = $"{name} description",
        IsLatest = true,
        Links = [new(string.Empty, "publish", string.Empty)]
    };

    private static System.Text.Json.Nodes.JsonObject CreateNativeCompensationScope() =>
        System.Text.Json.Nodes.JsonNode.Parse("""{"id":"scope-a","nodeId":"scope-a","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}""")!.AsObject();

    [Fact]
    public async Task WorkspaceReplacementRejectsAnInFlightSaveResponse()
    {
        await using var context = CreateContext();
        var initialDefinition = CreateDefinition("initial", "version-1");
        var replacementDefinition = CreateDefinition("replacement", "version-1");
        var cut = RenderWorkspace(context, initialDefinition, initialDefinition);
        var editor = cut.FindComponent<WorkflowEditor>().Instance;
        var saveTask = InvokeSaveAsync(editor);
        var pendingSave = await context.Services.GetRequiredService<DelayedWorkflowDefinitionEditorService>().SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        cut.Render(parameters => parameters
            .Add(x => x.WorkflowDefinition, replacementDefinition)
            .Add(x => x.SelectedWorkflowDefinition, replacementDefinition));
        await pendingSave.CompleteSuccessAsync(CreateDefinition("stale response", "version-1"));
        await saveTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Same(replacementDefinition, editor.WorkflowDefinition);
        Assert.Same(replacementDefinition, cut.Instance.GetSelectedDefinition());
    }

    [Fact]
    public async Task WorkspaceReplacementRejectsAnInFlightRetractResponse()
    {
        await using var context = CreateContext();
        var initialDefinition = CreateDefinition("initial", "version-1");
        var replacementDefinition = CreateDefinition("replacement", "version-1");
        var cut = RenderWorkspace(context, initialDefinition, initialDefinition);
        var editor = cut.FindComponent<WorkflowEditor>().Instance;
        var retractTask = InvokeRetractAsync(editor);
        var pendingRetract = await context.Services.GetRequiredService<DelayedWorkflowDefinitionEditorService>().RetractStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        cut.Render(parameters => parameters
            .Add(x => x.WorkflowDefinition, replacementDefinition)
            .Add(x => x.SelectedWorkflowDefinition, replacementDefinition));
        await pendingRetract.CompleteSuccessAsync(CreateDefinition("stale response", "version-1"));
        await retractTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Same(replacementDefinition, editor.WorkflowDefinition);
        Assert.Same(replacementDefinition, cut.Instance.GetSelectedDefinition());
    }

    private static Task InvokeSaveAsync(WorkflowEditor editor)
    {
        var method = typeof(WorkflowEditor).GetMethod("SaveChangesAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (Task)method.Invoke(editor, new object?[] { false, false, false, null, null, null })!;
    }

    private static Task InvokeRetractAsync(WorkflowEditor editor)
    {
        var method = typeof(WorkflowEditor).GetMethod("RetractAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (Task)method.Invoke(editor, new object?[] { null, null })!;
    }

    private static Task InvokeCapabilityRefreshAsync(WorkflowDefinitionWorkspace workspace)
    {
        var method = typeof(WorkflowDefinitionWorkspace).GetMethod("RefreshNativeCompensationDraftCapabilityAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (Task)method.Invoke(workspace, [false])!;
    }

    private sealed class TestWorkflowEditor : WorkflowEditor
    {
        protected override Task OnAfterRenderAsync(bool firstRender) => Task.CompletedTask;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
        }
    }

    private sealed class TestCodeView : CodeView
    {
        protected override void OnInitialized()
        {
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
        }
    }

    private sealed class TestWorkflowDefinitionVersionViewer : WorkflowDefinitionVersionViewer
    {
        protected override Task OnInitializedAsync() => Task.CompletedTask;
        protected override Task OnParametersSetAsync() => Task.CompletedTask;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
        }
    }

    private sealed class TestWorkflowProperties : WorkflowProperties
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<MetadataComponent>(0);
            builder.AddAttribute(1, nameof(MetadataComponent.WorkflowDefinition), WorkflowDefinition);
            builder.AddAttribute(2, nameof(MetadataComponent.WorkflowDefinitionUpdated), WorkflowDefinitionUpdated);
            builder.CloseComponent();
        }
    }

    private sealed class EmptyActivityPickerComponentProvider : IActivityPickerComponentProvider
    {
        public RenderFragment GetActivityPickerComponent() => _ => { };
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

    private sealed class MutableBackendApiClientProvider(string contextKey) : IBackendApiClientProvider, IBackendRequestContextBinding
    {
        private readonly AsyncLocal<string?> _boundContext = new();
        public Uri Url { get; } = new("https://backend.example/");
        public string ContextKey { get; set; } = contextKey;
        public bool SupportsAuthenticatedRequestContextBinding => true;
        public bool HasActiveRequestContextBinding => _boundContext.Value != null;

        public void RegisterAuthenticatedRequestTransport(IBackendRequestContextEpochSource? epochSource) { }

        public IDisposable BindRequestContext(string expectedContextKey)
        {
            if (expectedContextKey != ContextKey)
                throw new BackendRequestContextChangedException();
            var previous = _boundContext.Value;
            _boundContext.Value = expectedContextKey;
            return new ActionScope(() => _boundContext.Value = previous);
        }

        public void ValidateBoundRequestContext(IBackendRequestContextEpochSource? requestEpochSource)
        {
            if (_boundContext.Value is { } expected && expected != ContextKey)
                throw new BackendRequestContextChangedException();
        }

        public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class =>
            throw new InvalidOperationException("The workspace lifecycle test must not create a backend client.");

        private sealed class ActionScope(Action dispose) : IDisposable
        {
            public void Dispose() => dispose();
        }
    }

    private sealed class EnabledRemoteFeatureProvider(bool enabled = true) : IRemoteFeatureProvider, IRemoteFeatureProviderWithFreshCheck
    {
        public bool Enabled { get; set; } = enabled;
        public Func<Task<bool>>? FreshCheck { get; set; }
        public TaskCompletionSource DeferredCheckStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int FreshCheckCount { get; private set; }

        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<IEnumerable<Elsa.Api.Client.Resources.Features.Models.FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<Elsa.Api.Client.Resources.Features.Models.FeatureDescriptor>>([]);

        public Task<bool> IsEnabledFreshAsync(string featureName, CancellationToken cancellationToken = default)
        {
            FreshCheckCount++;
            if (FreshCheck is { } freshCheck)
            {
                DeferredCheckStarted.TrySetResult();
                return freshCheck();
            }

            return Task.FromResult(Enabled);
        }
    }

    private sealed class DeferredFreshFeatureProvider : IRemoteFeatureProvider, IRemoteFeatureProviderWithFreshCheck
    {
        private readonly TaskCompletionSource<bool> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<IEnumerable<Elsa.Api.Client.Resources.Features.Models.FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<Elsa.Api.Client.Resources.Features.Models.FeatureDescriptor>>([]);

        public Task<bool> IsEnabledFreshAsync(string featureName, CancellationToken cancellationToken = default)
        {
            Started.TrySetResult();
            return _result.Task;
        }

        public void Complete(bool enabled) => _result.TrySetResult(enabled);
    }

    private class WorkflowDefinitionServiceProxy : DispatchProxy
    {
        private readonly Queue<TaskCompletionSource<WorkflowDefinition?>> _pendingFinds = new();

        public int FindByDefinitionIdCallCount { get; private set; }
        public Func<string, WorkflowDefinition?> FindResultFactory { get; set; } = definitionId => CreateDefinition("loaded", definitionId);
        public Exception? FindException { get; set; }

        public TaskCompletionSource<WorkflowDefinition?> EnqueueFind()
        {
            var pending = NewCompletionSource<WorkflowDefinition?>();
            _pendingFinds.Enqueue(pending);
            return pending;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IWorkflowDefinitionService.FindByDefinitionIdAsync))
            {
                FindByDefinitionIdCallCount++;
                var definitionId = (string)args![0]!;
                if (_pendingFinds.TryDequeue(out var pending))
                    return pending.Task;

                if (FindException != null)
                    return Task.FromException<WorkflowDefinition?>(FindException);

                return Task.FromResult(FindResultFactory(definitionId));
            }

            if (targetMethod?.Name == nameof(IWorkflowDefinitionService.GetIsNameUniqueAsync))
                return Task.FromResult(true);

            throw new InvalidOperationException($"Unexpected call to {targetMethod?.Name}.");
        }
    }

    private sealed class DelayedWorkflowDefinitionEditorService : IWorkflowDefinitionEditorService, IContextBoundWorkflowDefinitionEditorService
    {
        public TaskCompletionSource<PendingSave> SaveStarted { get; } = NewCompletionSource<PendingSave>();
        public TaskCompletionSource<PendingRetract> RetractStarted { get; } = NewCompletionSource<PendingRetract>();

        public Task<Result<SaveWorkflowDefinitionResponse, ValidationErrors>> SaveAsync(WorkflowDefinition workflowDefinition, bool publish, Func<WorkflowDefinition, Task>? workflowSavedCallback = null, CancellationToken cancellationToken = default)
        {
            var pending = new PendingSave(workflowSavedCallback);
            SaveStarted.TrySetResult(pending);
            return pending.Completion.Task;
        }

        public Task<Result<SaveWorkflowDefinitionResponse, ValidationErrors>> SaveNativeCompensationDraftAsync(WorkflowDefinition workflowDefinition, bool publish, string expectedBackendContextKey, Func<WorkflowDefinition, Task>? workflowSavedCallback = null, CancellationToken cancellationToken = default) =>
            SaveAsync(workflowDefinition, publish, workflowSavedCallback, cancellationToken);

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

        public async Task CompleteSuccessAsync(WorkflowDefinition definition)
        {
            if (callback != null)
                await callback(definition);

            Completion.TrySetResult(new(new SaveWorkflowDefinitionResponse(definition, false, 0)));
        }
    }

    private sealed class PendingRetract(Func<WorkflowDefinition, Task>? callback)
    {
        public TaskCompletionSource<Result<WorkflowDefinition, ValidationErrors>> Completion { get; } = NewCompletionSource<Result<WorkflowDefinition, ValidationErrors>>();

        public async Task CompleteSuccessAsync(WorkflowDefinition definition)
        {
            if (callback != null)
                await callback(definition);

            Completion.TrySetResult(new(definition));
        }
    }


    private static TaskCompletionSource<T> NewCompletionSource<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
