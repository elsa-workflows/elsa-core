using Elsa.Diagnostics.ConsoleLogs.Permissions;

namespace Elsa.Diagnostics.ConsoleLogs.IntegrationTests;

public class ConsoleLogsHubAuthorizationTests
{
    [Fact]
    public void Permission_AppliesToHubAndEndpointContract()
    {
        Assert.Equal("read:diagnostics:console-logs", ConsoleLogsPermissions.Read);
    }
}
