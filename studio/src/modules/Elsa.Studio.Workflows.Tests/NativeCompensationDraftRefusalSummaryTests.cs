using Bunit;
using Elsa.Studio.Localization;
using Elsa.Studio.Testing;
using Elsa.Studio.Workflows.Components;
using Elsa.Studio.Workflows.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public sealed class NativeCompensationDraftRefusalSummaryTests : BunitContext, IAsyncLifetime
{
    public NativeCompensationDraftRefusalSummaryTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void RendersTheStableAccessibleRefusalSummaryAndFocusesIt()
    {
        var refusal = new NativeCompensationDraftRefusal([
            new(
                "scope-17",
                "step-9",
                NativeCompensationDraftDiagnosticRole.Forward,
                ["forward-activity-3"],
                ["capture-2"],
                "capture-2")
        ]);

        var cut = Render<NativeCompensationDraftRefusalSummary>(parameters => parameters
            .Add(x => x.Refusal, refusal)
            .Add(x => x.DefinitionId, "definition-42"));

        var summary = cut.Find("[aria-label='Native compensation publication refusal']");
        Assert.Equal("alert", summary.GetAttribute("role"));
        Assert.Equal("-1", summary.GetAttribute("tabindex"));
        Assert.Contains("Native compensation drafts cannot be published.", summary.TextContent);
        Assert.Contains("definition-42", summary.TextContent);
        Assert.Contains("scope-17", summary.TextContent);
        Assert.Contains("step-9", summary.TextContent);
        Assert.Contains("forward-activity-3", summary.TextContent);
        Assert.Contains("capture-2", summary.TextContent);
        Assert.Contains(JSInterop.Invocations, invocation =>
            invocation.Identifier == "Blazor._internal.domWrapper.focus");
    }

    [Fact]
    public void RendersDiagnosticIdentifiersAsEncodedText()
    {
        const string hostileIdentifier = "<img src=x onerror=alert(1)>";
        var refusal = new NativeCompensationDraftRefusal([
            new(
                "scope-17",
                "step-9",
                NativeCompensationDraftDiagnosticRole.Forward,
                [hostileIdentifier],
                [],
                "<svg onload=alert(2)>")
        ]);

        var cut = Render<NativeCompensationDraftRefusalSummary>(parameters => parameters
            .Add(x => x.Refusal, refusal));

        Assert.Contains("&lt;img src=x onerror=alert(1)&gt;", cut.Markup);
        Assert.Contains("&lt;svg onload=alert(2)&gt;", cut.Markup);
        Assert.Empty(cut.FindAll("img[onerror], svg[onload]"));
    }
}
