using Bunit;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Localization.Time;
using Elsa.Studio.Testing;
using Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public sealed class ActivityExecutionsTabTests : BunitContext, IAsyncLifetime
{
    public ActivityExecutionsTabTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<ITimeFormatter, TestTimeFormatter>();
        Render<MudPopoverProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public async Task ClickingAnExecutionRow_RaisesExecutionSelectedWithThatRecordId()
    {
        string? selectedId = null;
        var summaries = new List<ActivityExecutionRecordSummary>
        {
            CreateSummary("exec-a"),
            CreateSummary("exec-b")
        };

        var cut = Render<ActivityExecutionsTab>(parameters => parameters
            .Add(tab => tab.ActivityExecutionSummaries, summaries)
            .Add(tab => tab.VisiblePaneHeight, 400)
            .Add(tab => tab.ExecutionSelected, id => selectedId = id));

        var rows = cut.FindAll("tbody tr");
        Assert.Equal(2, rows.Count);

        await rows[1].ClickAsync();

        cut.WaitForAssertion(() => Assert.Equal("exec-b", selectedId));
    }

    private static ActivityExecutionRecordSummary CreateSummary(string id)
    {
        return new ActivityExecutionRecordSummary
        {
            Id = id,
            ActivityId = "WriteLine1",
            ActivityNodeId = "Workflow1:WriteLine1",
            ActivityType = "Elsa.WriteLine",
            Status = ActivityStatus.Completed,
            StartedAt = DateTimeOffset.UtcNow
        };
    }

    private sealed class TestTimeFormatter : ITimeFormatter
    {
        public string Format(DateTimeOffset? value, string format = "G", string emptyString = "") =>
            value?.ToString(format) ?? emptyString;
    }
}
