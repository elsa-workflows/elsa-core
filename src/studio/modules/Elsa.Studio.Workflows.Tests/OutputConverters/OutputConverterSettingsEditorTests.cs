using System.Text.Json;
using System.Text.Json.Nodes;
using Bunit;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs.Outputs.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests.OutputConverters;

public sealed class OutputConverterSettingsEditorTests : BunitContext, IAsyncLifetime
{
    public OutputConverterSettingsEditorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Render<MudPopoverProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public async Task RawSettingsRejectANonObjectWithoutChangingTheBinding()
    {
        JsonObject? changed = null;
        var cut = Render<OutputConverterSettingsEditor>(parameters => parameters
            .Add(x => x.Settings, new JsonObject { ["format"] = "compact" })
            .Add(x => x.SettingsChanged, value =>
            {
                changed = value;
                return Task.CompletedTask;
            }));

        var field = cut.FindComponent<MudTextField<string>>();
        await cut.InvokeAsync(() => field.Instance.ValueChanged.InvokeAsync("[]"));

        Assert.Null(changed);
        Assert.Contains("Converter settings must be a valid JSON object.", cut.Markup);
    }

    [Fact]
    public async Task SupportedObjectSchemaEditsTypedSettings()
    {
        JsonObject? changed = null;
        using var document = JsonDocument.Parse("""
            {"type":"object","properties":{"format":{"type":"string","title":"Format","default":"compact"}}}
            """);
        var cut = Render<OutputConverterSettingsEditor>(parameters => parameters
            .Add(x => x.SettingsSchema, document.RootElement.Clone())
            .Add(x => x.Settings, new JsonObject { ["format"] = "compact" })
            .Add(x => x.SettingsChanged, value =>
            {
                changed = value;
                return Task.CompletedTask;
            }));

        var field = cut.FindComponent<MudTextField<string>>();
        await cut.InvokeAsync(() => field.Instance.ValueChanged.InvokeAsync("indented"));

        Assert.Equal("indented", changed!["format"]!.GetValue<string>());
    }

    [Fact]
    public async Task TypedSettingsShowValidationErrorsWithoutChangingTheBinding()
    {
        JsonObject? changed = null;
        using var document = JsonDocument.Parse("""
            {"type":"object","properties":{"precision":{"type":"integer"}}}
            """);
        var cut = Render<OutputConverterSettingsEditor>(parameters => parameters
            .Add(x => x.SettingsSchema, document.RootElement.Clone())
            .Add(x => x.SettingsChanged, value =>
            {
                changed = value;
                return Task.CompletedTask;
            }));

        var field = cut.FindComponent<MudTextField<string>>();
        await cut.InvokeAsync(() => field.Instance.ValueChanged.InvokeAsync("not-an-integer"));

        Assert.Null(changed);
        Assert.Contains("Enter a valid integer.", cut.Markup);
    }

    [Fact]
    public void SchemaDefaultsAreNotDisplayedBeforeTheyArePersisted()
    {
        JsonObject? changed = null;
        using var document = JsonDocument.Parse("""
            {"type":"object","properties":{"format":{"type":"string","default":"compact"}}}
            """);
        var cut = Render<OutputConverterSettingsEditor>(parameters => parameters
            .Add(x => x.SettingsSchema, document.RootElement.Clone())
            .Add(x => x.Settings, new JsonObject())
            .Add(x => x.SettingsChanged, value =>
            {
                changed = value;
                return Task.CompletedTask;
            }));

        Assert.True(string.IsNullOrEmpty(cut.Find("input").GetAttribute("value")));
        Assert.Null(changed);
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }
}
