using System.Text.Json.Nodes;
using Bunit;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.DiagramDesigners.StateMachines.Presentation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests.StateMachines;

public sealed class BooleanConditionEditorDialogTests : BunitContext, IAsyncLifetime
{
    private readonly ExpressionServiceStub _expressionService;

    public BooleanConditionEditorDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        _expressionService = new ExpressionServiceStub();
        Services.AddSingleton<IExpressionService>(_expressionService);
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public async Task MissingCondition_OpensAsAlways_AndCancelDoesNotProduceAnEdit()
    {
        var (cut, reference) = await RenderDialog(null);

        Assert.Contains("Always", cut.Find("button.state-machine-condition-editor__mode--selected").TextContent);
        Assert.Empty(cut.FindAll("[data-testid='condition-editor-lossy-warning']"));

        cut.Find("button[data-testid='condition-editor-cancel']").Click();
        Assert.True((await reference.Result)?.Canceled);
    }

    [Fact]
    public async Task NeverMode_ProducesCanonicalFalseCondition_OnlyOnApply()
    {
        var original = JsonNode.Parse("true")!;
        var (cut, reference) = await RenderDialog(original);

        Assert.Contains("Always", cut.Find("button.state-machine-condition-editor__mode--selected").TextContent);
        cut.Find("button[data-mode='never']").Click();
        cut.Find("button[data-testid='condition-editor-apply']").Click();

        var result = Assert.IsType<BooleanConditionDialogResult>((await reference.Result)?.Data);
        Assert.True(result.Applied);
        Assert.False(result.Condition!["expression"]!["value"]!.GetValue<bool>());
        Assert.Equal("Boolean", result.Condition["typeName"]!.GetValue<string>());
        Assert.True(original.GetValue<bool>());
    }

    [Fact]
    public async Task ExplicitTrue_IsPreservedByDefault_AndClearedOnlyByExplicitChoice()
    {
        var original = JsonNode.Parse("{\"typeName\":\"Boolean\",\"expression\":{\"type\":\"Literal\",\"value\":true},\"metadata\":\"keep\"}")!;
        var (cut, reference) = await RenderDialog(original);

        cut.Find("button[data-testid='condition-editor-apply']").Click();

        var preserved = Assert.IsType<BooleanConditionDialogResult>((await reference.Result)?.Data);
        Assert.Same(original, preserved.Condition);

        var (clearCut, clearReference) = await RenderDialog(original);
        clearCut.Find("input[data-testid='condition-editor-preserve-explicit-true']").Change(false);
        Assert.NotEmpty(clearCut.FindAll("[data-testid='condition-editor-lossy-warning']"));
        clearCut.Find("button[data-testid='condition-editor-apply']").Click();

        var cleared = Assert.IsType<BooleanConditionDialogResult>((await clearReference.Result)?.Data);
        Assert.Null(cleared.Condition);
        Assert.Equal("keep", original["metadata"]!.GetValue<string>());
    }

    [Fact]
    public async Task InvalidCustomJson_DisablesApply_AndPreservesOriginal()
    {
        var original = JsonNode.Parse("{\"type\":\"Unknown\",\"value\":\"opaque\"}")!;
        var (cut, reference) = await RenderDialog(original);

        Assert.Contains("opaque", cut.Find("textarea[data-testid='condition-editor-custom-json']").GetAttribute("value"));
        cut.Find("textarea[data-testid='condition-editor-custom-json']").Input("{ broken");

        Assert.NotEmpty(cut.FindAll("[data-testid='condition-editor-custom-error']"));
        Assert.True(cut.Find("button[data-testid='condition-editor-apply']").HasAttribute("disabled"));
        Assert.False(reference.Result.IsCompleted);
        Assert.Equal("opaque", original["value"]!.GetValue<string>());
    }

    [Fact]
    public async Task SwitchingAwayFromProvider_ShowsLossyWarning_AndApplyAlwaysClearsCondition()
    {
        var original = JsonNode.Parse("{\"typeName\":\"Boolean\",\"expression\":{\"type\":\"JavaScript\",\"value\":\"value > 0\"}}")!;
        var (cut, reference) = await RenderDialog(original);

        cut.Find("button[data-mode='always']").Click();

        Assert.NotEmpty(cut.FindAll("[data-testid='condition-editor-lossy-warning']"));
        cut.Find("button[data-testid='condition-editor-apply']").Click();

        var result = Assert.IsType<BooleanConditionDialogResult>((await reference.Result)?.Data);
        Assert.Null(result.Condition);
        Assert.NotNull(original["expression"]);
    }

    [Fact]
    public async Task ProviderMode_UsesKnownProviderAndAppliesCanonicalExpression()
    {
        var (cut, reference) = await RenderDialog(null, providers:
        [
            new ExpressionDescriptor("JavaScript", "JavaScript"),
            new ExpressionDescriptor("Liquid", "Liquid")
        ]);

        cut.Find("button[data-mode='provider']").Click();
        cut.Find("textarea[data-testid='condition-editor-provider-expression']").Input("order.Total > 0");
        cut.Find("button[data-testid='condition-editor-apply']").Click();

        var result = Assert.IsType<BooleanConditionDialogResult>((await reference.Result)?.Data);
        Assert.Equal("JavaScript", result.Condition!["expression"]!["type"]!.GetValue<string>());
        Assert.Equal("order.Total > 0", result.Condition["expression"]!["value"]!.GetValue<string>());
    }

    [Fact]
    public async Task ChangingProviderType_ShowsLossyWarning()
    {
        var original = JsonNode.Parse("{\"typeName\":\"Boolean\",\"expression\":{\"type\":\"JavaScript\",\"value\":\"value > 0\"}}")!;
        var (cut, _) = await RenderDialog(original, providers:
        [
            new ExpressionDescriptor("JavaScript", "JavaScript"),
            new ExpressionDescriptor("Liquid", "Liquid")
        ]);

        cut.Find("select[data-testid='condition-editor-provider']").Change("Liquid");

        Assert.NotEmpty(cut.FindAll("[data-testid='condition-editor-lossy-warning']"));
    }

    [Fact]
    public async Task ReadOnlyCondition_DisablesEditingControls()
    {
        var (cut, reference) = await RenderDialog(JsonValue.Create(false), true);

        Assert.All(cut.FindAll("button[role='tab']"), button => Assert.True(button.HasAttribute("disabled")));
        Assert.True(cut.Find("button[data-testid='condition-editor-apply']").HasAttribute("disabled"));
        cut.Find("button[data-testid='condition-editor-cancel']").Click();
        Assert.True((await reference.Result)?.Canceled);
    }

    [Fact]
    public async Task ProviderLoadFailure_ShowsRecoverableWarningAndKeepsCustomJsonAvailable()
    {
        _expressionService.ThrowOnList = true;
        var original = JsonNode.Parse("{\"typeName\":\"Boolean\",\"expression\":{\"type\":\"Liquid\",\"value\":\"order.total > 0\"}}")!;

        var (cut, reference) = await RenderDialog(original, providers: null, provideKnownProviders: false);

        Assert.NotEmpty(cut.FindAll("[data-testid='condition-editor-provider-load-warning']"));
        Assert.Contains("Custom JSON", cut.Find("button[data-mode='custom']").TextContent);
        Assert.Equal("custom", cut.Find("button.state-machine-condition-editor__mode--selected").GetAttribute("data-mode"));
        var renderedSource = cut.Find("textarea[data-testid='condition-editor-custom-json']").GetAttribute("value");
        Assert.NotNull(renderedSource);
        Assert.True(JsonNode.DeepEquals(original, JsonNode.Parse(renderedSource!)));

        cut.Find("button[data-testid='condition-editor-cancel']").Click();
        Assert.True((await reference.Result)?.Canceled);
    }

    private async Task<(IRenderedComponent<MudDialogProvider> Host, IDialogReference Reference)> RenderDialog(
        JsonNode? condition,
        bool readOnly = false,
        IReadOnlyCollection<ExpressionDescriptor>? providers = null,
        bool provideKnownProviders = true)
    {
        var host = Render<MudDialogProvider>();
        var parameters = new DialogParameters<BooleanConditionEditorDialog>
        {
            { x => x.Condition, condition },
            { x => x.IsReadOnly, readOnly }
        };
        if (provideKnownProviders)
            parameters.Add(x => x.KnownExpressionProviders, providers ?? [new ExpressionDescriptor("JavaScript", "JavaScript")]);
        var reference = await Services.GetRequiredService<IDialogService>().ShowAsync<BooleanConditionEditorDialog>("Edit condition", parameters);
        host.WaitForElement("[data-testid='condition-editor']");
        return (host, reference);
    }

    private sealed class ExpressionServiceStub : IExpressionService
    {
        public bool ThrowOnList { get; set; }

        public Task<IEnumerable<ExpressionDescriptor>> ListDescriptorsAsync(CancellationToken cancellationToken = default) =>
            ThrowOnList
                ? Task.FromException<IEnumerable<ExpressionDescriptor>>(new HttpRequestException("provider service unavailable"))
                : Task.FromResult<IEnumerable<ExpressionDescriptor>>([new("JavaScript", "JavaScript")]);

        public Task<ExpressionDescriptor?> GetByTypeAsync(string type, CancellationToken cancellationToken = default) =>
            Task.FromResult<ExpressionDescriptor?>(new(type, type));
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }
}
