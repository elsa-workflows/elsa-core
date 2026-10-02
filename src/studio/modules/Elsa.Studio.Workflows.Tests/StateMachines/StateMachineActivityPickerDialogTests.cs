using Bunit;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.DiagramDesigners.StateMachines.Presentation;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Services;
using Elsa.Studio.Workflows.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests.StateMachines;

public sealed class StateMachineActivityPickerDialogTests : BunitContext, IAsyncLifetime
{
    private readonly ActivityDescriptor _writeLine = new()
    {
        Name = "WriteLine",
        DisplayName = "Write line",
        TypeName = "Elsa.WriteLine",
        Category = "Primitives",
        Description = "Writes text to the log.",
        IsBrowsable = true
    };

    private readonly ActivityDescriptor _http = new()
    {
        Name = "HttpEndpoint",
        DisplayName = "HTTP endpoint",
        TypeName = "Elsa.HttpEndpoint",
        Category = "HTTP",
        Description = "Waits for an incoming HTTP request.",
        IsBrowsable = true
    };

    private readonly ActivityDescriptor _sequence = new()
    {
        Name = "Sequence",
        DisplayName = "Sequence",
        TypeName = "Elsa.Sequence",
        Category = "Composition",
        Description = "Runs a sequence of activities.",
        IsBrowsable = false
    };

    private readonly ActivityDescriptor _flowchart = new()
    {
        Name = "Flowchart",
        DisplayName = "Flowchart",
        TypeName = "Elsa.Flowchart",
        Category = "Composition",
        Description = "Runs activities as a graph.",
        IsBrowsable = false
    };

    private readonly ActivityDescriptor _stateMachine = new()
    {
        Name = "StateMachine",
        DisplayName = "State machine",
        TypeName = "Elsa.StateMachine",
        Category = "Composition",
        Description = "Runs state and transition driven workflows.",
        IsBrowsable = false
    };

    private readonly ActivityDescriptor _internalActivity = new()
    {
        Name = "InternalActivity",
        DisplayName = "Internal activity",
        TypeName = "Elsa.InternalActivity",
        Category = "Internal",
        IsBrowsable = false
    };

    private readonly IRenderedComponent<MudDialogProvider> _dialogProvider;

    public StateMachineActivityPickerDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<IWorkflowRootActivityTemplateProvider, DefaultWorkflowRootActivityTemplateProvider>();
        Services.AddSingleton<IActivityRegistry>(new TestActivityRegistry([_writeLine, _http, _sequence, _flowchart, _stateMachine, _internalActivity]));
        Render<MudPopoverProvider>();
        _dialogProvider = Render<MudDialogProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public async Task Picker_LoadsBrowsableActivitiesAndFiltersEverySearchField()
    {
        var dialog = await ShowDialogAsync();

        _dialogProvider.WaitForAssertion(() => Assert.NotEmpty(_dialogProvider.FindAll("[data-testid='state-machine-activity-option']")));
        _dialogProvider.FindAll("button").Single(x => x.TextContent.Contains("All activities", StringComparison.Ordinal)).Click();
        Assert.Equal(5, _dialogProvider.FindAll("[data-activity-type]").Count);
        Assert.DoesNotContain(_dialogProvider.FindAll("[data-activity-type]"), element => element.GetAttribute("data-activity-type") == _internalActivity.TypeName);
        Assert.Contains(_dialogProvider.FindAll("[data-activity-type]"), element => element.GetAttribute("data-activity-type") == _flowchart.TypeName);
        Assert.Contains(_dialogProvider.FindAll("[data-activity-type]"), element => element.GetAttribute("data-activity-type") == _stateMachine.TypeName);
        var search = _dialogProvider.Find("input[id*='state-machine-activity-picker']");

        search.Input("incoming");
        Assert.Single(_dialogProvider.FindAll("[data-testid='state-machine-activity-option']"));
        Assert.Contains("HTTP endpoint", _dialogProvider.Find("[data-testid='state-machine-activity-option']").TextContent);

        search.Input("Elsa.WriteLine");
        Assert.Single(_dialogProvider.FindAll("[data-testid='state-machine-activity-option']"));
        Assert.Contains("Write line", _dialogProvider.Find("[data-testid='state-machine-activity-option']").TextContent);

        search.Input("Primitives");
        Assert.Single(_dialogProvider.FindAll("[data-testid='state-machine-activity-option']"));

        search.Input("missing activity");
        Assert.Contains("No matching activities", _dialogProvider.Markup);

        _dialogProvider.FindAll("button").Single(x => x.TextContent.Trim() == "Cancel").Click();
        Assert.True((await dialog.Result)?.Canceled);
    }

    [Fact]
    public async Task Picker_ReturnsDescriptorOnlyAfterExplicitSelection()
    {
        var dialog = await ShowDialogAsync();

        Assert.False(dialog.Result.IsCompleted);
        _dialogProvider.WaitForAssertion(() => Assert.NotEmpty(_dialogProvider.FindAll("[data-testid='state-machine-activity-option']")));
        _dialogProvider.FindAll("button").Single(x => x.TextContent.Contains("All activities", StringComparison.Ordinal)).Click();
        _dialogProvider.Find("[data-testid='state-machine-activity-option'][data-activity-type='Elsa.WriteLine']").Click();

        Assert.False(dialog.Result.IsCompleted);
        Assert.Contains("Write line", _dialogProvider.Find(".state-machine-activity-picker__details").TextContent);
        _dialogProvider.Find("[data-testid='state-machine-activity-picker-commit']").Click();

        var result = await dialog.Result;
        Assert.False(result?.Canceled);
        Assert.Same(_writeLine, result?.Data);
    }

    [Fact]
    public async Task Picker_OffersDesignerBackedRootsAndRecommendsSequence()
    {
        var dialog = await ShowDialogAsync();

        _dialogProvider.WaitForAssertion(() => Assert.NotEmpty(_dialogProvider.FindAll("[data-activity-type='Elsa.Sequence']")));
        Assert.Equal(3, _dialogProvider.FindAll("[data-testid='state-machine-activity-option']").Count);
        Assert.NotEmpty(_dialogProvider.FindAll("[data-activity-type='Elsa.Flowchart']"));
        Assert.NotEmpty(_dialogProvider.FindAll("[data-activity-type='Elsa.StateMachine']"));
        var sequence = _dialogProvider.Find("[data-activity-type='Elsa.Sequence']");
        Assert.Contains("Recommended", sequence.TextContent);
        sequence.Click();
        Assert.False(dialog.Result.IsCompleted);
        _dialogProvider.Find("[data-testid='state-machine-activity-picker-commit']").Click();

        var result = await dialog.Result;
        Assert.Same(_sequence, result?.Data);
    }

    [Theory]
    [InlineData("trigger", "Choose a trigger activity", "WHEN", "Starts the transition")]
    [InlineData("action", "Choose an action activity", "THEN", "Runs after source exit")]
    [InlineData("entry", "Choose an entry activity", "ON ENTRY", "Runs when this state becomes active")]
    [InlineData("exit", "Choose an exit activity", "ON EXIT", "Runs before an accepted transition leaves this state")]
    public async Task Picker_ExplainsTheSlotContext(string slotName, string heading, string kicker, string description)
    {
        var dialog = await ShowDialogAsync(slotName);

        _dialogProvider.WaitForAssertion(() => Assert.Contains(heading, _dialogProvider.Markup));
        Assert.Contains(kicker, _dialogProvider.Markup);
        Assert.Contains(description, _dialogProvider.Markup);

        _dialogProvider.FindAll("button").Single(x => x.TextContent.Trim() == "Cancel").Click();
        Assert.True((await dialog.Result)?.Canceled);
    }

    [Fact]
    public async Task Picker_ExposesRecentAndCategoryViewsAndSupportsKeyboardCommit()
    {
        var dialog = await ShowDialogAsync("action", ["Elsa.WriteLine"]);

        _dialogProvider.WaitForAssertion(() => Assert.NotEmpty(_dialogProvider.FindAll("button")));
        var recent = _dialogProvider.FindAll("button").Single(x => x.TextContent.Contains("Recent", StringComparison.Ordinal));
        Assert.False(recent.HasAttribute("disabled"));
        recent.Click();
        Assert.Single(_dialogProvider.FindAll("[data-testid='state-machine-activity-option']"));
        Assert.Contains("Write line", _dialogProvider.Find("[data-testid='state-machine-activity-option']").TextContent);

        _dialogProvider.Find("input[id*='state-machine-activity-picker']").KeyDown("Enter");
        var result = await dialog.Result;
        Assert.Same(_writeLine, result?.Data);
    }

    [Fact]
    public async Task Picker_EnterOnAFilterDoesNotCommitTheCurrentSelection()
    {
        var dialog = await ShowDialogAsync();

        _dialogProvider.WaitForAssertion(() => Assert.NotEmpty(_dialogProvider.FindAll("button")));
        var allActivities = _dialogProvider.FindAll("button").Single(x => x.TextContent.Contains("All activities", StringComparison.Ordinal));
        allActivities.KeyDown("Enter");

        Assert.False(dialog.Result.IsCompleted);
    }

    [Fact]
    public async Task Picker_SlashShortcutRefocusesSearchFromAFilterWithoutChangingTheSearchText()
    {
        var dialog = await ShowDialogAsync();

        _dialogProvider.WaitForAssertion(() => Assert.NotEmpty(_dialogProvider.FindAll("[data-testid='state-machine-activity-option']")));
        var search = _dialogProvider.Find("input[id*='state-machine-activity-picker']");
        var allActivities = _dialogProvider.FindAll("button").Single(x => x.TextContent.Contains("All activities", StringComparison.Ordinal));
        Assert.Equal("/", _dialogProvider.Find("[data-testid='state-machine-activity-picker']").GetAttribute("aria-keyshortcuts"));
        var initialFocusCalls = JSInterop.Invocations.Count(x => x.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));

        allActivities.KeyDown("/");

        _dialogProvider.WaitForAssertion(() =>
        {
            var focusCalls = JSInterop.Invocations.Count(x => x.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));
            Assert.True(focusCalls > initialFocusCalls);
        });
        Assert.Equal(string.Empty, search.GetAttribute("value"));
        Assert.False(dialog.Result.IsCompleted);
    }

    [Fact]
    public async Task Picker_UsesReplacementLanguageWhenReplacingAnActivity()
    {
        var dialog = await ShowDialogAsync(isReplacing: true);

        _dialogProvider.WaitForAssertion(() => Assert.NotEmpty(_dialogProvider.FindAll("[data-testid='state-machine-activity-option']")));
        _dialogProvider.FindAll("button").Single(x => x.TextContent.Contains("All activities", StringComparison.Ordinal)).Click();
        _dialogProvider.Find("[data-testid='state-machine-activity-option'][data-activity-type='Elsa.WriteLine']").Click();

        Assert.Contains("Replace with Write line", _dialogProvider.Find("[data-testid='state-machine-activity-picker-commit']").TextContent);
        Assert.False(dialog.Result.IsCompleted);
    }

    /// <summary>
    /// A host other than the StateMachine designer can narrow what the picker offers and say what the choice is for; the
    /// default offering (browsable activities plus designer-backed roots) must not leak past the filter.
    /// </summary>
    [Fact]
    public async Task Picker_OffersOnlyWhatTheHostsFilterAccepts_InTheHostsOwnContext()
    {
        var dialog = await ShowDialogAsync(configure: parameters =>
        {
            parameters.Add(x => x.DescriptorFilter, descriptor => descriptor.TypeName is "Elsa.WriteLine" or "Elsa.InternalActivity");
            parameters.Add(x => x.ContextLabel, "PERFORMED BY");
            parameters.Add(x => x.ContextHint, "Runs when the process reaches 'Notify Warehouse'");
        });

        _dialogProvider.WaitForAssertion(() => Assert.NotEmpty(_dialogProvider.FindAll("[data-testid='state-machine-activity-option']")));
        Assert.Equal(["Elsa.InternalActivity", "Elsa.WriteLine"], _dialogProvider.FindAll("[data-activity-type]").Select(x => x.GetAttribute("data-activity-type")).Order());
        Assert.Contains("PERFORMED BY", _dialogProvider.Markup);
        Assert.Contains("Runs when the process reaches 'Notify Warehouse'", _dialogProvider.Markup);
        Assert.DoesNotContain("THEN", _dialogProvider.Markup);

        _dialogProvider.FindAll("button").Single(x => x.TextContent.Trim() == "Cancel").Click();
        Assert.True((await dialog.Result)?.Canceled);
    }

    private async Task<IDialogReference> ShowDialogAsync(
        string slotName = "action",
        IReadOnlyCollection<string>? recentActivityTypes = null,
        bool isReplacing = false,
        Action<DialogParameters<StateMachineActivityPickerDialog>>? configure = null)
    {
        var dialogService = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters<StateMachineActivityPickerDialog>
        {
            { x => x.SlotName, slotName },
            { x => x.IsReplacing, isReplacing },
            { x => x.RecentActivityTypes, recentActivityTypes ?? [] }
        };
        configure?.Invoke(parameters);
        var article = slotName == "trigger" ? "a" : "an";
        return await _dialogProvider.InvokeAsync(() => dialogService.ShowAsync<StateMachineActivityPickerDialog>($"Choose {article} {slotName} activity", parameters));
    }

}
