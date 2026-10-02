using Xunit;

namespace Elsa.Studio.Workflows.Tests.StateMachines;

public sealed class StateMachineCanvasLifecycleContractTests
{
    [Fact]
    public void Wrapper_RemountsForReadOnlyChangesAndCarriesSelectionAcrossViews()
    {
        var wrapper = ReadAsset("StateMachineDesignerWrapper.razor");
        var canvas = ReadAsset("StateMachineCanvas.razor.cs");

        Assert.Contains("@key=\"IsReadOnly\"", wrapper, StringComparison.Ordinal);
        Assert.Contains("SelectedVisualId=\"@(_selectedStateId ?? _selectedTransitionId)\"", wrapper, StringComparison.Ordinal);
        Assert.Contains("[Parameter] public string? SelectedVisualId", canvas, StringComparison.Ordinal);
        Assert.Contains("await _graphApi.SelectCellAsync(SelectedVisualId)", canvas, StringComparison.Ordinal);
    }

    [Fact]
    public void ProgrammaticSelection_DoesNotMasqueradeAsUserSelection()
    {
        var bindings = ReadAsset("graph-bindings.ts");
        var selection = ReadAsset("select-cell.ts");
        var graph = ReadAsset("create-graph.ts");

        Assert.Contains("suppressSelectionCallbacks?: number", bindings, StringComparison.Ordinal);
        Assert.Contains("binding.suppressSelectionCallbacks =", selection, StringComparison.Ordinal);
        Assert.Contains("graph.resetSelection(cell)", selection, StringComparison.Ordinal);
        Assert.Contains("binding.suppressSelectionCallbacks--", selection, StringComparison.Ordinal);
        Assert.Contains("graphBindings[graphId]?.suppressSelectionCallbacks", graph, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadOnlyMode_DoesNotExposeOrExecuteAutoLayout()
    {
        var wrapper = ReadAsset("StateMachineDesignerWrapper.razor");
        var canvas = ReadAsset("StateMachineCanvas.razor.cs");

        Assert.Contains("@if (!_showOutline && !IsReadOnly)", wrapper, StringComparison.Ordinal);
        Assert.Contains("public Task AutoLayoutAsync() => IsReadOnly", canvas, StringComparison.Ordinal);
    }

    [Fact]
    public void Wrapper_ExposesSeparateStateAndTransitionCreationMenus()
    {
        var wrapper = ReadAsset("StateMachineDesignerWrapper.razor");
        const string menuStart = "<details class=\"state-machine-designer__create-menu\" name=\"state-machine-create\">";

        Assert.Equal(2, wrapper.Split(menuStart, StringSplitOptions.None).Length - 1);
        Assert.Contains("<summary>@Localizer[\"Add state\"]</summary>", wrapper, StringComparison.Ordinal);
        Assert.Contains("<summary>@Localizer[\"Add transition\"]</summary>", wrapper, StringComparison.Ordinal);
        Assert.DoesNotContain("<summary>@Localizer[\"Add\"]</summary>", wrapper, StringComparison.Ordinal);

        var stateMenuStart = wrapper.IndexOf("<summary>@Localizer[\"Add state\"]</summary>", StringComparison.Ordinal);
        var stateMenuEnd = wrapper.IndexOf("</details>", stateMenuStart, StringComparison.Ordinal);
        Assert.True(stateMenuStart >= 0 && stateMenuEnd > stateMenuStart);
        var stateMenu = wrapper[stateMenuStart..stateMenuEnd];
        Assert.Contains("State name", stateMenu, StringComparison.Ordinal);
        Assert.DoesNotContain("Transition name", stateMenu, StringComparison.Ordinal);

        var transitionMenuStart = wrapper.IndexOf("<summary>@Localizer[\"Add transition\"]</summary>", StringComparison.Ordinal);
        var transitionMenuEnd = wrapper.IndexOf("</details>", transitionMenuStart, StringComparison.Ordinal);
        Assert.True(transitionMenuStart >= 0 && transitionMenuEnd > transitionMenuStart);
        var transitionMenu = wrapper[transitionMenuStart..transitionMenuEnd];
        Assert.Contains("Transition name", transitionMenu, StringComparison.Ordinal);
        Assert.DoesNotContain("State name", transitionMenu, StringComparison.Ordinal);
    }

    [Fact]
    public void TransitionInspector_UsesSemanticPickerCallbacksAndExcludesConditionFromActivityDrops()
    {
        var wrapper = ReadAsset("StateMachineDesignerWrapper.razor");

        Assert.Contains("ActivityAddRequested=\"AddTransitionActivityAsync\"", wrapper, StringComparison.Ordinal);
        Assert.Contains("ActivityOpenRequested=\"OpenTransitionActivityAsync\"", wrapper, StringComparison.Ordinal);
        Assert.Contains("ActivityReplaceRequested=\"ReplaceTransitionActivityAsync\"", wrapper, StringComparison.Ordinal);
        Assert.Contains("ActivityClearRequested=\"ClearTransitionSlotAsync\"", wrapper, StringComparison.Ordinal);
        Assert.Contains("ActivityDropRequested=\"OnTransitionSlotDropAsync\"", wrapper, StringComparison.Ordinal);
    }

    private static string ReadAsset(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "DesignerAssets", name));
}
