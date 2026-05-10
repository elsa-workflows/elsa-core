using Elsa.Diagnostics.StructuredLogs.Extensions;
using Elsa.Diagnostics.StructuredLogs.Permissions;

namespace Elsa.Diagnostics.StructuredLogs.UnitTests;

public class StructuredLogsNamingTests
{
    [Fact]
    public void Permission_UsesDiagnosticsStructuredLogsName()
    {
        Assert.Equal("read:diagnostics:structured-logs", StructuredLogsPermissions.Read);
    }

    [Fact]
    public void HubRoute_UsesDiagnosticsStructuredLogsPath()
    {
        Assert.Equal("/elsa/hubs/diagnostics/structured-logs", EndpointRouteBuilderExtensions.HubRoute);
    }
}
