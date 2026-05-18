using Elsa.Diagnostics.ConsoleLogs.Extensions;
using Elsa.Diagnostics.ConsoleLogs.Permissions;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests;

public class ConsoleLogsNamingTests
{
    [Fact]
    public void Permission_UsesDiagnosticsConsoleLogsName()
    {
        Assert.Equal("read:diagnostics:console-logs", ConsoleLogsPermissions.Read);
    }

    [Fact]
    public void HubRoute_UsesDiagnosticsConsoleLogsPath()
    {
        Assert.Equal("/elsa/hubs/diagnostics/console-logs", EndpointRouteBuilderExtensions.HubRoute);
    }
}
