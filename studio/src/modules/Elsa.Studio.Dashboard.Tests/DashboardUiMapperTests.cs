using Elsa.Studio.Dashboard.Models;
using Elsa.Studio.Dashboard.Services;
using MudBlazor;
using Xunit;

namespace Elsa.Studio.Dashboard.Tests;

public class DashboardUiMapperTests
{
    [Theory]
    [InlineData(DashboardRuntimeStatusKeys.AcceptingWork, Color.Success, "Accepting work")]
    [InlineData(DashboardRuntimeStatusKeys.Paused, Color.Warning, "Paused")]
    [InlineData(DashboardRuntimeStatusKeys.Draining, Color.Info, "Draining")]
    [InlineData(DashboardRuntimeStatusKeys.Unavailable, Color.Error, "Unavailable")]
    public void RuntimeMapping_ReturnsSemanticColorAndLabel(string status, Color expectedColor, string expectedLabel)
    {
        Assert.Equal(expectedColor, DashboardUiMapper.RuntimeColor(status));
        Assert.Equal(expectedLabel, DashboardUiMapper.RuntimeLabel(status));
    }

    [Theory]
    [InlineData(DashboardFindingSeverity.Success, Severity.Success, Color.Success)]
    [InlineData(DashboardFindingSeverity.Warning, Severity.Warning, Color.Warning)]
    [InlineData(DashboardFindingSeverity.Error, Severity.Error, Color.Error)]
    [InlineData(DashboardFindingSeverity.Critical, Severity.Error, Color.Error)]
    [InlineData(DashboardFindingSeverity.Info, Severity.Info, Color.Info)]
    public void SeverityMapping_ReturnsSemanticSeverityAndColor(string severity, Severity expectedSeverity, Color expectedColor)
    {
        Assert.Equal(expectedSeverity, DashboardUiMapper.Severity(severity));
        Assert.Equal(expectedColor, DashboardUiMapper.SeverityColor(severity));
        Assert.False(string.IsNullOrWhiteSpace(DashboardUiMapper.SeverityIcon(severity)));
    }

    [Theory]
    [InlineData("Available", "Available", Color.Success)]
    [InlineData("Unauthorized", "No access", Color.Warning)]
    [InlineData("NotInstalled", "Not installed", Color.Default)]
    [InlineData("Unavailable", "Unavailable", Color.Error)]
    public void CapabilityMapping_ReturnsOperatorLabelAndColor(string status, string expectedLabel, Color expectedColor)
    {
        var capability = new DashboardCapabilityStatus(status);

        Assert.Equal(expectedLabel, DashboardUiMapper.CapabilityLabel(capability));
        Assert.Equal(expectedColor, DashboardUiMapper.CapabilityColor(capability));
    }
}
