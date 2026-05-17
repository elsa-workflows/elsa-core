using Elsa.Diagnostics.ConsoleLogs.Permissions;
using Elsa.Diagnostics.ConsoleLogs.RealTime;
using Microsoft.AspNetCore.Authorization;

namespace Elsa.Diagnostics.ConsoleLogs.IntegrationTests;

public class ConsoleLogsAuthorizationTests
{
    [Fact]
    public void HubAuthorization_UsesDedicatedPermission()
    {
        var authorize = Assert.Single(typeof(ConsoleLogsHub).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>());

        Assert.Equal(ConsoleLogsPermissions.Read, authorize.Policy);
    }
}
