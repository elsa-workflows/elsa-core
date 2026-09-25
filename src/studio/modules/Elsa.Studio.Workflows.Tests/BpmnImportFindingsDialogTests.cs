using Bunit;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionList;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers <see cref="BpmnImportFindingsDialog"/>: it groups findings by severity with each finding's element id, and
/// -- when the document declares more than one process -- withholds confirmation until one is chosen. A Dropped
/// finding is the thing a user most needs to see before committing to the import, so the test pins that it renders
/// rather than being summarized away.
/// </summary>
public sealed class BpmnImportFindingsDialogTests : BunitContext, IAsyncLifetime
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private readonly IRenderedComponent<MudDialogProvider> _dialogProvider;

    public BpmnImportFindingsDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Render<MudPopoverProvider>();
        _dialogProvider = Render<MudDialogProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public async Task TheDialogRendersFindingsGroupedBySeverityWithElementIds()
    {
        var analysis = new BpmnImportAnalysisModel
        {
            ProcessIds = ["process-1"],
            Issues =
            [
                new() { Severity = "Dropped", ElementId = "Gateway_1", Message = "The condition expression could not be represented." },
                new() { Severity = "Degraded", ElementId = "Task_2", Message = "The timer definition was approximated." },
                new() { Severity = "Info", ElementId = "StartEvent_1", Message = "The start event was imported." }
            ]
        };

        await ShowDialogAsync(analysis);

        Assert.Contains("Gateway_1", _dialogProvider.Markup);
        Assert.Contains("The condition expression could not be represented.", _dialogProvider.Markup);
        Assert.Contains("Task_2", _dialogProvider.Markup);
        Assert.Contains("StartEvent_1", _dialogProvider.Markup);
        Assert.Contains("Dropped (1)", _dialogProvider.Markup);
        Assert.Contains("Degraded (1)", _dialogProvider.Markup);
        Assert.Contains("Info (1)", _dialogProvider.Markup);
    }

    [Fact]
    public async Task TheDialogClosesImmediately_WhenTheDocumentDeclaresExactlyOneProcess()
    {
        // A single process needs no prompt: the dialog pre-selects it so Import is enabled without the user having
        // to choose anything.
        var analysis = new BpmnImportAnalysisModel { ProcessIds = ["only-process"] };
        var dialog = await ShowDialogAsync(analysis);

        Assert.False(ImportButton().HasAttribute("disabled"));

        await ClickImport();

        var result = await dialog.Result.WaitAsync(Timeout);
        Assert.False(result?.Canceled);
        Assert.Equal("only-process", result?.Data);
    }

    [Fact]
    public async Task TheDialogWithholdsConfirmation_UntilAProcessIsChosen_WhenThereAreSeveral()
    {
        var analysis = new BpmnImportAnalysisModel { ProcessIds = ["process-a", "process-b"] };
        var dialog = await ShowDialogAsync(analysis);

        Assert.True(ImportButton().HasAttribute("disabled"));

        await SelectProcessAsync("process-b");
        await ClickImport();

        var result = await dialog.Result.WaitAsync(Timeout);
        Assert.False(result?.Canceled);
        Assert.Equal("process-b", result?.Data);
    }

    [Fact]
    public async Task TheDialogCancelsWithoutClosingAsConfirmed()
    {
        var dialog = await ShowDialogAsync(new BpmnImportAnalysisModel { ProcessIds = ["only-process"] });

        await _dialogProvider.InvokeAsync(() => CancelButton().ClickAsync(new MouseEventArgs()));

        var result = await dialog.Result.WaitAsync(Timeout);
        Assert.True(result?.Canceled);
    }

    private async Task<IDialogReference> ShowDialogAsync(BpmnImportAnalysisModel analysis)
    {
        var dialogService = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters<BpmnImportFindingsDialog> { { x => x.Analysis, analysis } };
        var dialog = await _dialogProvider.InvokeAsync(() => dialogService.ShowAsync<BpmnImportFindingsDialog>("Import BPMN", parameters));
        _dialogProvider.WaitForElement("button");
        return dialog;
    }

    private Task SelectProcessAsync(string processId) => _dialogProvider.InvokeAsync(() =>
        _dialogProvider.FindComponent<MudSelect<string>>().Instance.ValueChanged.InvokeAsync(processId));

    private Task ClickImport() => _dialogProvider.InvokeAsync(() => ImportButton().ClickAsync(new MouseEventArgs()));

    private AngleSharp.Dom.IElement ImportButton() => _dialogProvider.FindAll("button").Single(x => x.TextContent.Trim() == "Import");

    private AngleSharp.Dom.IElement CancelButton() => _dialogProvider.FindAll("button").Single(x => x.TextContent.Trim() == "Cancel");

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }
}
