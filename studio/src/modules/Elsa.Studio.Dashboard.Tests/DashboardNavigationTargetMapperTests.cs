using Elsa.Studio.Dashboard.Services;
using Xunit;

namespace Elsa.Studio.Dashboard.Tests;

public class DashboardNavigationTargetMapperTests
{
    [Theory]
    [InlineData("running", "workflows/instances?status=Running")]
    [InlineData("faulted", "workflows/instances?subStatus=Faulted&hasIncidents=true")]
    [InlineData("suspended", "workflows/instances?subStatus=Suspended")]
    [InlineData("interrupted", "workflows/instances?subStatus=Interrupted")]
    [InlineData("incidents", "workflows/instances?hasIncidents=true")]
    public void ForMetric_ReturnsWorkflowInstanceTarget(string metric, string expected)
    {
        var result = DashboardNavigationTargetMapper.ForMetric(metric);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ForWorkflowInstance_EscapesInstanceId()
    {
        var result = DashboardNavigationTargetMapper.ForWorkflowInstance("instance/1");

        Assert.Equal("workflows/instances/instance%2F1/view", result);
    }

    [Theory]
    [InlineData("WorkflowInstances", "faulted", "workflows/instances?subStatus=Faulted&hasIncidents=true")]
    [InlineData("StructuredLogs", "errors", "diagnostics/structured-logs?level=Error")]
    [InlineData("ConsoleLogs", "dropped", "diagnostics/console?stream=stderr")]
    [InlineData("Runtime", "runtime", null)]
    public void ForFinding_ReturnsRelevantTarget(string targetKind, string target, string? expected)
    {
        var result = DashboardNavigationTargetMapper.ForFinding(targetKind, target);

        Assert.Equal(expected, result);
    }
}
