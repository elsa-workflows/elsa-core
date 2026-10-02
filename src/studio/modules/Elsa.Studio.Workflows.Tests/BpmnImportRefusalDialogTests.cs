using Bunit;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionList;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers <see cref="BpmnImportRefusalDialog"/>: a BPMN import refused for missing host capabilities (a <c>422</c>
/// from <c>bpmn/import</c>) is shown here instead of squeezed into a snackbar, so the test pins that the capability
/// names and offending element ids the refusal carries actually render.
/// </summary>
public sealed class BpmnImportRefusalDialogTests : BunitContext, IAsyncLifetime
{
    private readonly IRenderedComponent<MudDialogProvider> _dialogProvider;

    public BpmnImportRefusalDialogTests()
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
    public async Task TheDialogRendersTheCapabilityNamesAndElementIds()
    {
        var refusal = new BpmnCapabilityRefusal(["ScopeSignalling", "CompensationHandling"], ["Gateway_1", "Task_2"]);
        var dialogService = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters<BpmnImportRefusalDialog> { { x => x.Refusal, refusal } };
        await _dialogProvider.InvokeAsync(() => dialogService.ShowAsync<BpmnImportRefusalDialog>("Import refused", parameters));
        _dialogProvider.WaitForElement("button");

        Assert.Contains("ScopeSignalling", _dialogProvider.Markup);
        Assert.Contains("CompensationHandling", _dialogProvider.Markup);
        Assert.Contains("Gateway_1", _dialogProvider.Markup);
        Assert.Contains("Task_2", _dialogProvider.Markup);
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }
}
