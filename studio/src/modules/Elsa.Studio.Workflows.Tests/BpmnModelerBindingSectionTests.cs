using System.Text.Json.Nodes;
using Bunit;
using Elsa.Studio.Bpmn.Designers;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs;
using Elsa.Studio.Workflows.DiagramDesigners.Bpmn;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.Tests.Support;
using Microsoft.AspNetCore.Components.Rendering;
using MudBlazor;
using Xunit;
using static Elsa.Studio.Workflows.Tests.Support.BpmnDocumentFixtures;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers the "Performed by" section of the BPMN designer's panel: it reads a task's binding through the designer's
/// <c>getBinding</c>, writes picks and input edits through <c>setBinding</c> and nothing else, keeps the draft being
/// edited when the binding read back is its own, and follows the document when an undo or a reload changes it.
/// </summary>
public sealed class BpmnModelerBindingSectionTests : BunitContext, IAsyncLifetime
{
    private static readonly BpmnDesignerSelectedElement Task = new(TaskId, "bpmn:ServiceTask", "Notify Warehouse");

    private static readonly BpmnDesignerBinding WriteLineBinding =
        new("Elsa.WriteLine", [new("text", """{"typeName":"String","expression":{"type":"Literal","value":"Notifying the warehouse"}}""")]);

    private readonly FakeModelerCanvas _canvas = new();
    private readonly FakeBpmnXmlService _service = new();
    private readonly IRenderedComponent<MudDialogProvider> _dialogProvider;
    private readonly BpmnModelerSession _session;

    public BpmnModelerBindingSectionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddBpmnEditing(new FakeBpmnDocumentService());
        ComponentFactories.Add<InputsTab, InputsTabStandIn>();
        Render<MudPopoverProvider>();
        _dialogProvider = Render<MudDialogProvider>();

        _session = new(_service.ReturnsXml("<definitions/>", "\"REVISION-1\""), DefinitionId);
        _session.EnsureLoadedAsync().GetAwaiter().GetResult();
        _session.AttachCanvas(_canvas);
        _session.CanvasLoaded(new(true, null, []), _session.DocumentVersion);
        _canvas.BindingApplied = elementId => _session.CanvasChanged(new([elementId], IsDirty: true, CanUndo: true, CanRedo: false));
        _canvas.Bindings[TaskId] = WriteLineBinding;
    }

    Task IAsyncLifetime.InitializeAsync() => System.Threading.Tasks.Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact(DisplayName = "A bound task shows its activity and that activity's own input editors, read through getBinding")]
    public void ShowsTheBoundActivity()
    {
        var cut = RenderSection(Task);

        Assert.Contains("Write Line (Elsa.WriteLine)", cut.Find("[data-testid='bpmn-bound-activity']").TextContent);
        var inputs = cut.FindComponent<InputsTab>().Instance;
        Assert.Equal("Elsa.WriteLine", inputs.ActivityDescriptor!.TypeName);
        Assert.Equal("Notifying the warehouse", inputs.Activity!["text"]!["expression"]!["value"]!.GetValue<string>());
    }

    [Fact(DisplayName = "Editing an input writes the binding through setBinding and keeps the editors open")]
    public async Task EditingAnInput_SetsTheBinding()
    {
        var cut = RenderSection(Task);
        var inputs = cut.FindComponent<InputsTab>().Instance;

        inputs.Activity!["text"] = JsonNode.Parse("""{"typeName":"String","expression":{"type":"JavaScript","value":"getMessage()"}}""");
        await cut.InvokeAsync(() => inputs.OnActivityUpdated!(inputs.Activity!));

        var (elementId, binding) = Assert.Single(_canvas.SetBindings);
        Assert.Equal(TaskId, elementId);
        Assert.Equal("Elsa.WriteLine", binding.ActivityType);
        Assert.Contains("getMessage()", Assert.Single(binding.Inputs).Value);
        Assert.True(_session.IsDirty);
        Assert.Same(inputs, cut.FindComponent<InputsTab>().Instance);
    }

    [Fact(DisplayName = "Picking an activity binds it through setBinding and shows its inputs")]
    public async Task PickingAnActivity_SetsTheBinding()
    {
        var cut = RenderSection(Task);

        await cut.InvokeAsync(() => cut.Find("[data-testid='bpmn-pick-activity']").Click());
        _dialogProvider.WaitForAssertion(() => Assert.NotEmpty(_dialogProvider.FindAll("[data-testid='state-machine-activity-option']")));
        _dialogProvider.FindAll("button").Single(button => button.TextContent.Contains("All activities", StringComparison.Ordinal)).Click();
        _dialogProvider.Find("[data-testid='state-machine-activity-option'][data-activity-type='Elsa.HttpRequest']").Click();
        _dialogProvider.Find("[data-testid='state-machine-activity-picker-commit']").Click();

        cut.WaitForAssertion(() => Assert.Equal("Elsa.HttpRequest", Assert.Single(_canvas.SetBindings).Binding.ActivityType));
        cut.WaitForAssertion(() => Assert.Equal("Elsa.HttpRequest", cut.FindComponent<InputsTab>().Instance.ActivityDescriptor!.TypeName));
    }

    [Fact(DisplayName = "An undo that changes the binding replaces the draft with the binding read back")]
    public async Task AChangeFromTheDesigner_ReplacesTheDraft()
    {
        var cut = RenderSection(Task);
        _canvas.Bindings[TaskId] = new("Elsa.Delay", []);

        await cut.InvokeAsync(() => _session.CanvasChanged(new([TaskId], IsDirty: false, CanUndo: false, CanRedo: true)));

        cut.WaitForAssertion(() => Assert.Equal("Elsa.Delay", cut.FindComponent<InputsTab>().Instance.ActivityDescriptor!.TypeName));
    }

    [Fact(DisplayName = "A task the designer refuses to bind says why, and the document keeps its binding")]
    public async Task ARefusal_IsShown()
    {
        var cut = RenderSection(Task);
        _canvas.SetBindingRefusal = BpmnDesignerSetBindingRefusals.NotBindable;
        var inputs = cut.FindComponent<InputsTab>().Instance;

        await cut.InvokeAsync(() => inputs.OnActivityUpdated!(inputs.Activity!));

        Assert.Contains("sends or waits for a message", cut.Find("[data-testid='bpmn-binding-refused']").TextContent);
        Assert.Same(WriteLineBinding, _canvas.Bindings[TaskId]);
        Assert.False(_session.IsDirty);
    }

    [Fact(DisplayName = "A pick the designer refuses is not shown; the activity the document keeps is")]
    public async Task ARefusedPick_ShowsTheKeptBinding()
    {
        var cut = RenderSection(Task);
        _canvas.SetBindingRefusal = BpmnDesignerSetBindingRefusals.NotBindable;

        await cut.InvokeAsync(() => cut.Find("[data-testid='bpmn-pick-activity']").Click());
        _dialogProvider.WaitForAssertion(() => Assert.NotEmpty(_dialogProvider.FindAll("[data-testid='state-machine-activity-option']")));
        _dialogProvider.FindAll("button").Single(button => button.TextContent.Contains("All activities", StringComparison.Ordinal)).Click();
        _dialogProvider.Find("[data-testid='state-machine-activity-option'][data-activity-type='Elsa.HttpRequest']").Click();
        _dialogProvider.Find("[data-testid='state-machine-activity-picker-commit']").Click();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid='bpmn-binding-refused']")));
        Assert.Contains("Write Line (Elsa.WriteLine)", cut.Find("[data-testid='bpmn-bound-activity']").TextContent);
        Assert.Equal("Elsa.WriteLine", cut.FindComponent<InputsTab>().Instance.ActivityDescriptor!.TypeName);
    }

    [Fact(DisplayName = "A read that started before an input edit does not replace the editors while the edit is being applied")]
    public async Task AReadFromBeforeAnEdit_DoesNotReplaceTheEditors()
    {
        var cut = RenderSection(Task);
        var inputs = cut.FindComponent<InputsTab>().Instance;
        var staleRead = _canvas.NextGetBindingGate = new();
        await cut.InvokeAsync(() => _session.CanvasChanged(new(["Shape_1"], IsDirty: true, CanUndo: true, CanRedo: false)));

        var write = _canvas.NextSetBindingGate = new();
        inputs.Activity!["text"] = JsonNode.Parse("""{"typeName":"String","expression":{"type":"Literal","value":"Edited"}}""");
        var edit = cut.InvokeAsync(() => inputs.OnActivityUpdated!(inputs.Activity!));
        await cut.InvokeAsync(staleRead.SetResult);
        Assert.Same(inputs, cut.FindComponent<InputsTab>().Instance);

        await cut.InvokeAsync(write.SetResult);
        await edit;

        Assert.Same(inputs, cut.FindComponent<InputsTab>().Instance);
        Assert.Contains("Edited", Assert.Single(_canvas.Bindings[TaskId].Inputs).Value);
    }

    [Fact(DisplayName = "While the diagram is read again, the editors are hidden and the picker is off; then the binding of the document shown is read")]
    public async Task WhileTheDiagramIsReadAgain_NothingCanBeEdited()
    {
        var cut = RenderSection(Task);
        _service.ReturnsXml("<definitions/>", "\"REVISION-2\"");
        _service.GetGate = new();

        var reload = System.Threading.Tasks.Task.CompletedTask;
        await cut.InvokeAsync(() => { reload = _session.ReloadAsync(); });
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid='bpmn-binding-loading']")));
        Assert.Empty(cut.FindComponents<InputsTab>());
        Assert.True(cut.Find("[data-testid='bpmn-pick-activity']").HasAttribute("disabled"));

        await cut.InvokeAsync(_service.GetGate.SetResult);
        await reload;
        _canvas.Bindings[TaskId] = new("Elsa.Delay", []);
        await cut.InvokeAsync(() => _session.CanvasLoaded(new(true, null, []), _session.DocumentVersion));

        cut.WaitForAssertion(() => Assert.Equal("Elsa.Delay", cut.FindComponent<InputsTab>().Instance.ActivityDescriptor!.TypeName));
        Assert.False(cut.Find("[data-testid='bpmn-pick-activity']").HasAttribute("disabled"));
    }

    [Fact(DisplayName = "An unbound task offers the picker")]
    public void AnUnboundTask_OffersThePicker()
    {
        _canvas.Bindings.Clear();

        var cut = RenderSection(Task);

        Assert.Equal("Not bound", cut.Find("[data-testid='bpmn-bound-activity']").TextContent.Trim());
        Assert.Equal("Choose activity", cut.Find("[data-testid='bpmn-pick-activity']").TextContent.Trim());
        Assert.Empty(cut.FindComponents<InputsTab>());
    }

    [Fact(DisplayName = "An element that is not a task performs no work, and nothing is read for it")]
    public void AGateway_PerformsNoWork()
    {
        var cut = RenderSection(new("Gateway_1", "bpmn:ExclusiveGateway", "Approved?"));

        Assert.NotEmpty(cut.FindAll("[data-testid='bpmn-no-work']"));
        Assert.Empty(cut.FindAll("[data-testid='bpmn-pick-activity']"));
        Assert.Empty(_canvas.SetBindings);
    }

    [Fact(DisplayName = "A binding whose input is not JSON is reported, not shown as editable")]
    public void AnUnreadableBinding_IsReported()
    {
        _canvas.Bindings[TaskId] = new("Elsa.WriteLine", [new("text", "{not json")]);

        var cut = RenderSection(Task);

        Assert.Contains("is not JSON", cut.Find("[data-testid='bpmn-binding-unreadable']").TextContent);
        Assert.Empty(cut.FindComponents<InputsTab>());
    }

    private IRenderedComponent<BpmnModelerBindingSection> RenderSection(BpmnDesignerSelectedElement element) =>
        Render<BpmnModelerBindingSection>(parameters => parameters
            .Add(x => x.Session, _session)
            .Add(x => x.Element, element));

    /// <summary>Stands in for the real input editors, which need a browser, keeping the parameters the section hands it.</summary>
    private sealed class InputsTabStandIn : InputsTab
    {
        protected override Task OnParametersSetAsync() => System.Threading.Tasks.Task.CompletedTask;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
        }
    }
}
