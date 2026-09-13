using Elsa.Diagnostics.StructuredLogs.Extensions;
using Elsa.Diagnostics.StructuredLogs.Permissions;
using System.Threading.Tasks;

namespace Elsa.Diagnostics.StructuredLogs.UnitTests;

public class StructuredLogsNamingTests
{
    [Test]
    // Pins the resource name, not the legacy permission string: the convention worth holding is that
    // this module is called 'diagnostics/structured-logs' wherever it appears.
    public async Task Permission_UsesDiagnosticsStructuredLogsName()
    {
        await Assert.That(StructuredLogsResourcePermissions.StructuredLogs).IsEqualTo("diagnostics/structured-logs");
    }

    [Test]
    public async Task HubRoute_UsesDiagnosticsStructuredLogsPath()
    {
        await Assert.That(EndpointRouteBuilderExtensions.HubRoute).IsEqualTo("/elsa/hubs/diagnostics/structured-logs");
    }
}
