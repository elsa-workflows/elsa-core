using Bunit;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.DiagramDesigners.Bpmn;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers <see cref="ExportBpmnDialog"/>: the design requires disclosing, before the download starts, that the
/// exported file carries the definition's binding configuration and expressions.
/// </summary>
public sealed class ExportBpmnDialogTests : BunitContext, IAsyncLifetime
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private readonly IRenderedComponent<MudDialogProvider> _dialogProvider;

    public ExportBpmnDialogTests()
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
    public async Task TheDialogDisclosesThatTheExportCarriesBindingConfigurationAndExpressions()
    {
        await ShowDialogAsync();

        Assert.Contains("binding configuration and expressions", _dialogProvider.Markup);
    }

    [Fact]
    public async Task TheDialogClosesConfirmed_WhenExportIsClicked()
    {
        var dialog = await ShowDialogAsync();

        await _dialogProvider.InvokeAsync(() => ExportButton().ClickAsync(new MouseEventArgs()));

        var result = await dialog.Result.WaitAsync(Timeout);
        Assert.False(result?.Canceled);
        Assert.True(result?.Data as bool?);
    }

    [Fact]
    public async Task TheDialogCancels_WhenCancelIsClicked()
    {
        var dialog = await ShowDialogAsync();

        await _dialogProvider.InvokeAsync(() => CancelButton().ClickAsync(new MouseEventArgs()));

        var result = await dialog.Result.WaitAsync(Timeout);
        Assert.True(result?.Canceled);
    }

    private async Task<IDialogReference> ShowDialogAsync()
    {
        var dialogService = Services.GetRequiredService<IDialogService>();
        var dialog = await _dialogProvider.InvokeAsync(() => dialogService.ShowAsync<ExportBpmnDialog>("Export BPMN"));
        _dialogProvider.WaitForElement("button");
        return dialog;
    }

    private AngleSharp.Dom.IElement ExportButton() => _dialogProvider.FindAll("button").Single(x => x.TextContent.Trim() == "Export");
    private AngleSharp.Dom.IElement CancelButton() => _dialogProvider.FindAll("button").Single(x => x.TextContent.Trim() == "Cancel");

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }
}
