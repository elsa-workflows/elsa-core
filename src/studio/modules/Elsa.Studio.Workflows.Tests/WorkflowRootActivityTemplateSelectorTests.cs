using Bunit;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Services;
using Elsa.Studio.Workflows.Shared.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public sealed class WorkflowRootActivityTemplateSelectorTests : BunitContext, IAsyncLifetime
{
    private readonly IWorkflowRootActivityTemplateProvider _templateProvider = new DefaultWorkflowRootActivityTemplateProvider();
    private readonly IRenderedComponent<MudDialogProvider> _dialogProvider;

    public WorkflowRootActivityTemplateSelectorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton(_templateProvider);
        Render<MudPopoverProvider>();
        _dialogProvider = Render<MudDialogProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void Selector_RendersTheSelectedTemplateAsAnAccessibleRadio()
    {
        var cut = Render<WorkflowRootActivityTemplateSelector>(parameters => parameters
            .Add(x => x.Templates, _templateProvider.List())
            .Add(x => x.SelectedTemplateKey, DefaultWorkflowRootActivityTemplateProvider.SequenceKey)
            .Add(x => x.AriaLabel, "Branch type"));

        var selected = cut.FindAll("[role='radio']").Single(x => x.GetAttribute("aria-checked") == "true");

        Assert.Contains("Sequence", selected.TextContent);
        Assert.Equal("Branch type", cut.Find("[role='radiogroup']").GetAttribute("aria-label"));
    }

    [Fact]
    public async Task Dialog_CancelReturnsNoSelection()
    {
        var dialog = await ShowDialogAsync(DefaultWorkflowRootActivityTemplateProvider.SequenceKey);

        _dialogProvider.WaitForAssertion(() => Assert.Contains("Sequence", _dialogProvider.Markup));
        _dialogProvider.FindAll("button").Single(x => x.TextContent.Trim() == "Cancel").Click();
        var result = await dialog.Result;

        Assert.True(result?.Canceled);
    }

    [Fact]
    public async Task Dialog_ConfirmsTheChosenTemplate()
    {
        var dialog = await ShowDialogAsync(DefaultWorkflowRootActivityTemplateProvider.SequenceKey);

        _dialogProvider.WaitForAssertion(() =>
        {
            var selected = _dialogProvider.FindAll("[role='radio']").Single(x => x.GetAttribute("aria-checked") == "true");
            Assert.Contains("Sequence", selected.TextContent);
        });
        _dialogProvider.FindAll("[role='radio']").Single(x => x.TextContent.Contains("State machine")).Click();
        _dialogProvider.FindAll("button").Single(x => x.TextContent.Trim() == "Ok").Click();
        var result = await dialog.Result;

        Assert.False(result?.Canceled);
        Assert.Equal(DefaultWorkflowRootActivityTemplateProvider.StateMachineKey, result?.Data);
    }

    private async Task<IDialogReference> ShowDialogAsync(string selectedTemplateKey)
    {
        var dialogService = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters<SelectWorkflowRootActivityDialog>
        {
            { x => x.SelectedTemplateKey, selectedTemplateKey }
        };
        return await _dialogProvider.InvokeAsync(() =>
            dialogService.ShowAsync<SelectWorkflowRootActivityDialog>("Create Then branch", parameters));
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] =>
            new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }
}
