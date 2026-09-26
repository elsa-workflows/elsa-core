using Elsa.Studio.Diagnostics.ConsoleLogs.Models;
using Xunit;
using ConsoleLogViewerComponent = Elsa.Studio.Diagnostics.ConsoleLogs.UI.Components.ConsoleLogViewer;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Tests;

public class ConsoleLogsPageHelperTests
{
    [Fact]
    public void StripAnsi_RemovesCsiSequences()
    {
        var text = TestConsoleLogs.StripAnsiForTest("plain \u001b[31mred\u001b[0m text");

        Assert.Equal("plain red text", text);
    }

    [Fact]
    public void StripAnsi_RemovesNonSgrCsiSequences()
    {
        var text = TestConsoleLogs.StripAnsiForTest("before\u001b[2K after");

        Assert.Equal("before after", text);
    }

    [Fact]
    public void StripAnsi_RemovesOscSequencesTerminatedByBel()
    {
        var text = TestConsoleLogs.StripAnsiForTest("before\u001b]0;window title\a after");

        Assert.Equal("before after", text);
    }

    [Fact]
    public void StripAnsi_RemovesOscSequencesTerminatedByStringTerminator()
    {
        var text = TestConsoleLogs.StripAnsiForTest("before\u001b]8;;https://example.com\u001b\\link\u001b]8;;\u001b\\ after");

        Assert.Equal("beforelink after", text);
    }

    [Fact]
    public void StripAnsi_RemovesDcsSequences()
    {
        var text = TestConsoleLogs.StripAnsiForTest("before\u001bPpayload\u001b\\ after");

        Assert.Equal("before after", text);
    }

    [Fact]
    public void CreateExportFileName_UsesTimestamp()
    {
        var fileName = TestConsoleLogs.CreateExportFileNameForTest(DateTimeOffset.Parse("2026-05-18T09:10:11Z"));

        Assert.Equal("diagnostics-console-logs-20260518-091011.tsv", fileName);
    }

    [Fact]
    public async Task OnParametersSetAsync_WhenScopeChangesBeforeActivation_AppliesFilter()
    {
        var component = new TestConsoleLogs();

        await component.ApplyParametersForTestAsync(
            "workflow-instance-a",
            "activity-instance-a",
            "activity-a",
            "node-a");

        Assert.Equal("workflow-instance-a", component.Filter.WorkflowInstanceId);
        Assert.Equal("activity-instance-a", component.Filter.ActivityInstanceId);
        Assert.Equal("activity-a", component.Filter.ActivityId);
        Assert.Equal("node-a", component.Filter.ActivityNodeId);
    }

    private class TestConsoleLogs : ConsoleLogViewerComponent
    {
        public ConsoleLogFilter Filter => ViewState.Filter;

        public Task ApplyParametersForTestAsync(string workflowInstanceId, string activityInstanceId, string activityId, string activityNodeId)
        {
            WorkflowInstanceId = workflowInstanceId;
            ActivityInstanceId = activityInstanceId;
            ActivityId = activityId;
            ActivityNodeId = activityNodeId;
            return OnParametersSetAsync();
        }

        public static string StripAnsiForTest(string text) => StripAnsi(text);

        public static string CreateExportFileNameForTest(DateTimeOffset timestamp) => CreateExportFileName(timestamp);
    }
}
