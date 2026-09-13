using Elsa.Diagnostics.ConsoleLogs.Extensions;
using Elsa.Diagnostics.ConsoleLogs.Permissions;
using System.Threading.Tasks;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests;

public class ConsoleLogsNamingTests
{
    [Test]
    // Pins the resource name, not the legacy permission string: the convention worth holding is that
    // this module is called 'diagnostics/console-logs' wherever it appears.
    public async Task Permission_UsesDiagnosticsConsoleLogsName()
    {
        await Assert.That(ConsoleLogsResourcePermissions.ConsoleLogs).IsEqualTo("diagnostics/console-logs");
    }

    [Test]
    public async Task HubRoute_UsesDiagnosticsConsoleLogsPath()
    {
        await Assert.That(EndpointRouteBuilderExtensions.HubRoute).IsEqualTo("/elsa/hubs/diagnostics/console-logs");
    }
}