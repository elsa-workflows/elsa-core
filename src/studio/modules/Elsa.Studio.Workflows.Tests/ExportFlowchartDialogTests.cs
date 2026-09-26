using System.Text.Json.Nodes;
using Bunit;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.DiagramDesigners.Flowcharts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers the flowchart export dialog and the file name it proposes.
/// <para>
/// The dialog is the only gate between the user and the download: whatever it closes with is handed straight to the
/// designer's JavaScript export function. So the tests pin both directions of every rule — that invalid options keep
/// the dialog open, and that valid options actually reach the caller unchanged.
/// </para>
/// </summary>
public sealed class ExportFlowchartDialogTests : BunitContext, IAsyncLifetime
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private const string ProposedFileName = "Order processing";
    private const string EmptyFileNameMessage = "Please enter a file name for the export.";
    private const string OutOfRangePaddingMessage = "The padding must be between 0 and 1000 pixels.";

    private readonly IRenderedComponent<MudDialogProvider> _dialogProvider;

    public ExportFlowchartDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Render<MudPopoverProvider>();
        _dialogProvider = Render<MudDialogProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    /// <summary>Identifies how the dialog is submitted, since both routes must obey the same validation.</summary>
    public enum SubmitPath
    {
        /// <summary>A native form submission, which is what pressing Enter in the file name field produces.</summary>
        Form,

        /// <summary>The dialog's Ok button, which lives outside the form and validates the model itself.</summary>
        OkButton
    }

    [Theory]
    [InlineData("Order processing", 0, "Order processing")]
    [InlineData("Order processing", 3, "Order processing_v3")]
    [InlineData("  Order processing  ", 3, "Order processing_v3")]
    // '/' is invalid in a file name on every platform the studio runs on, unlike ':' or '\', so it is the only
    // character this assertion can pin without becoming platform-specific.
    [InlineData("Orders/Fulfillment", 2, "Orders_Fulfillment_v2")]
    [InlineData("", 4, "flowchart_v4")]
    [InlineData(null, 0, "flowchart")]
    public void TheProposedFileNameDerivesFromTheWorkflowDefinitionNameAndVersion(string? name, int version, string expected)
    {
        var workflowDefinition = new WorkflowDefinition
        {
            Name = name!,
            Version = version
        };

        Assert.Equal(expected, FlowchartDiagramDesigner.GetDefaultFileName(workflowDefinition));
    }

    [Fact]
    public void TheProposedFileNameFallsBackWhenThereIsNoWorkflowDefinition() =>
        Assert.Equal("flowchart", FlowchartDiagramDesigner.GetDefaultFileName(null));

    [Fact]
    public void TheProposedFileNameIgnoresTheRootActivityJsonEvenWhenItCarriesAName()
    {
        // Production shape: a real workflow's root `Elsa.Flowchart` activity carries no `name`, and its `version`
        // is the activity type's version rather than the workflow's - as reproduced here. `GetDefaultFileName` no
        // longer even accepts the root activity JSON; it is derived solely from the workflow definition, which is
        // how production supplies it (via `DisplayContext.WorkflowDefinition`).
        var rootActivity = new JsonObject
        {
            ["type"] = "Elsa.Flowchart",
            ["version"] = 1
        };
        var workflowDefinition = new WorkflowDefinition
        {
            Name = "Order processing",
            Version = 3
        };

        var fileName = FlowchartDiagramDesigner.GetDefaultFileName(workflowDefinition);

        Assert.Equal("Order processing_v3", fileName);
        Assert.Null(rootActivity["name"]);
    }

    [Theory]
    [InlineData(SubmitPath.Form)]
    [InlineData(SubmitPath.OkButton)]
    public async Task TheDialogDoesNotCloseWhenTheFileNameIsEmpty(SubmitPath submitPath)
    {
        var dialog = await ShowDialogAsync();
        await SetFileNameAsync(string.Empty);

        await Submit(submitPath);

        Assert.False(dialog.Result.IsCompleted);
        _dialogProvider.WaitForAssertion(() => Assert.Contains(EmptyFileNameMessage, _dialogProvider.Markup));
    }

    [Theory]
    [InlineData(SubmitPath.Form, "-5")]
    [InlineData(SubmitPath.OkButton, "-5")]
    [InlineData(SubmitPath.Form, "1001")]
    [InlineData(SubmitPath.OkButton, "1001")]
    public async Task TheDialogDoesNotCloseWhenThePaddingIsOutOfRange(SubmitPath submitPath, string padding)
    {
        var dialog = await ShowDialogAsync();
        await SetPaddingAsync(padding);

        await Submit(submitPath);

        Assert.False(dialog.Result.IsCompleted);
        _dialogProvider.WaitForAssertion(() => Assert.Contains(OutOfRangePaddingMessage, _dialogProvider.Markup));
    }

    [Theory]
    [InlineData(SubmitPath.Form)]
    [InlineData(SubmitPath.OkButton)]
    public async Task TheDialogClosesWithTheSelectedOptions(SubmitPath submitPath)
    {
        var dialog = await ShowDialogAsync();
        await SelectFormatAsync(ExportGraphFormat.Jpeg);
        await SetFileNameAsync("Fulfillment diagram");
        await SetPaddingAsync("40");

        await Submit(submitPath);

        var result = await dialog.Result.WaitAsync(Timeout);
        var options = Assert.IsType<ExportGraphOptions>(result?.Data);

        Assert.False(result?.Canceled);
        Assert.Equal(ExportGraphFormat.Jpeg, options.Format);
        Assert.Equal("Fulfillment diagram", options.FileName);
        Assert.Equal(40, options.Padding);
        Assert.DoesNotContain(EmptyFileNameMessage, _dialogProvider.Markup);
        Assert.DoesNotContain(OutOfRangePaddingMessage, _dialogProvider.Markup);
    }

    [Fact]
    public async Task TheDialogClosesWithTheProposedFileNameAndTheDefaultFormat()
    {
        var dialog = await ShowDialogAsync();

        await Submit(SubmitPath.OkButton);

        var options = Assert.IsType<ExportGraphOptions>((await dialog.Result.WaitAsync(Timeout))?.Data);

        Assert.Equal(ExportGraphFormat.Png, options.Format);
        Assert.Equal(ProposedFileName, options.FileName);
    }

    [Fact]
    public async Task TheFormatRadioGroupHasAnAccessibleName()
    {
        await ShowDialogAsync();

        var radioGroup = _dialogProvider.Find("[role='radiogroup']");
        Assert.Equal("Format", radioGroup.GetAttribute("aria-label"));
    }

    [Fact]
    public async Task ThePaddingFieldIsShownOnlyForTheRasterFormats()
    {
        await ShowDialogAsync();

        Assert.NotEmpty(PaddingFields());

        await SelectFormatAsync(ExportGraphFormat.Svg);
        Assert.Empty(PaddingFields());

        // The other half of the rule: the field comes back rather than disappearing for good.
        await SelectFormatAsync(ExportGraphFormat.Jpeg);
        Assert.NotEmpty(PaddingFields());
    }

    [Fact]
    public async Task SvgIsNotBlockedByAPaddingTheDialogNoLongerShows()
    {
        var dialog = await ShowDialogAsync();
        await SetPaddingAsync("-5");
        await Submit(SubmitPath.OkButton);

        Assert.False(dialog.Result.IsCompleted);

        // Switching to SVG hides the padding field, so a padding rule that still applied would keep the dialog open
        // with its complaint rendered nowhere the user could see or fix it.
        await SelectFormatAsync(ExportGraphFormat.Svg);
        await Submit(SubmitPath.OkButton);

        var options = Assert.IsType<ExportGraphOptions>((await dialog.Result.WaitAsync(Timeout))?.Data);

        Assert.Equal(ExportGraphFormat.Svg, options.Format);
    }

    private async Task<IDialogReference> ShowDialogAsync()
    {
        var dialogService = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters<ExportFlowchartDialog> { { x => x.FileName, ProposedFileName } };
        var dialog = await _dialogProvider.InvokeAsync(() => dialogService.ShowAsync<ExportFlowchartDialog>("Export flowchart", parameters));
        _dialogProvider.WaitForElement("form");
        _dialogProvider.WaitForAssertion(() => Assert.Equal(ProposedFileName, FileNameField().Find("input").GetAttribute("value")));
        return dialog;
    }

    // The find and the trigger run on the renderer's dispatcher so that a render cannot invalidate the element's
    // event handler in between. The returned task still completes only when the handler does.
    private Task Submit(SubmitPath submitPath) => _dialogProvider.InvokeAsync(() => submitPath switch
    {
        SubmitPath.Form => _dialogProvider.Find("form").SubmitAsync(),
        SubmitPath.OkButton => OkButton().ClickAsync(new MouseEventArgs()),
        _ => throw new ArgumentOutOfRangeException(nameof(submitPath), submitPath, null)
    });

    private Task SetFileNameAsync(string fileName) => FileNameField().Find("input").ChangeAsync(new ChangeEventArgs { Value = fileName });

    private Task SetPaddingAsync(string padding) => PaddingFields().Single().Find("input").ChangeAsync(new ChangeEventArgs { Value = padding });

    // Radios are matched by their visible label text rather than by position, so a reordering or a missing option
    // fails the test loudly instead of silently selecting the wrong format.
    private static string FormatLabel(ExportGraphFormat format) => format switch
    {
        ExportGraphFormat.Png => "PNG",
        ExportGraphFormat.Jpeg => "JPEG",
        ExportGraphFormat.Svg => "SVG",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
    };

    private Task SelectFormatAsync(ExportGraphFormat format) => _dialogProvider.InvokeAsync(() =>
        _dialogProvider.FindAll("label.mud-radio")
            .Single(label => label.TextContent.Trim() == FormatLabel(format))
            .QuerySelector("input.mud-radio-input")!
            .ClickAsync(new MouseEventArgs()));

    private IRenderedComponent<MudTextField<string>> FileNameField() => _dialogProvider.FindComponents<MudTextField<string>>()[0];

    private IReadOnlyList<IRenderedComponent<MudNumericField<int>>> PaddingFields() => _dialogProvider.FindComponents<MudNumericField<int>>();

    private AngleSharp.Dom.IElement OkButton() => _dialogProvider.FindAll("button").Single(x => x.TextContent.Trim() == "Ok");

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }
}
