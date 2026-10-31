using Elsa.Diagnostics.ConsoleLogs.Extensions;
using Elsa.Diagnostics.ConsoleLogs.Permissions;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests;

public class ConsoleLogsNamingTests
{
    [Fact]
    // Pins the resource name, not the legacy permission string: the convention worth holding is that
    // this module is called 'diagnostics/console-logs' wherever it appears.
    public void Permission_UsesDiagnosticsConsoleLogsName()
    {
        Assert.Equal("diagnostics/console-logs", ConsoleLogsResourcePermissions.ConsoleLogs);
    }

    [Fact]
    public void HubRoute_UsesDiagnosticsConsoleLogsPath()
    {
        Assert.Equal("/elsa/hubs/diagnostics/console-logs", EndpointRouteBuilderExtensions.HubRoute);
    }
}
