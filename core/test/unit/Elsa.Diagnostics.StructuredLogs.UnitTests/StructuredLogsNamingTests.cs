using Elsa.Diagnostics.StructuredLogs.Extensions;
using Elsa.Diagnostics.StructuredLogs.Permissions;

namespace Elsa.Diagnostics.StructuredLogs.UnitTests;

public class StructuredLogsNamingTests
{
    [Fact]
    // Pins the resource name, not the legacy permission string: the convention worth holding is that
    // this module is called 'diagnostics/structured-logs' wherever it appears.
    public void Permission_UsesDiagnosticsStructuredLogsName()
    {
        Assert.Equal("diagnostics/structured-logs", StructuredLogsResourcePermissions.StructuredLogs);
    }

    [Fact]
    public void HubRoute_UsesDiagnosticsStructuredLogsPath()
    {
        Assert.Equal("/elsa/hubs/diagnostics/structured-logs", EndpointRouteBuilderExtensions.HubRoute);
    }
}
