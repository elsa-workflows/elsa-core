using Elsa.Diagnostics.ConsoleLogs.Extensions;
using Elsa.Diagnostics.ConsoleLogs.Features;
using Elsa.Diagnostics.ConsoleLogs.Permissions;

namespace Elsa.Diagnostics.ConsoleLogs.IntegrationTests;

public class ConsoleLogsModuleTests
{
    [Test]
    public async Task Module_UsesDiagnosticsConsoleLogsIdentity()
    {
        await Assert.That(EndpointRouteBuilderExtensions.HubRoute).IsEqualTo("/elsa/hubs/diagnostics/console-logs");
        await Assert.That(ConsoleLogsResourcePermissions.ConsoleLogs).IsEqualTo("diagnostics/console-logs");
        await Assert.That(typeof(ConsoleLogsFeature).Namespace).StartsWith("Elsa.Diagnostics.ConsoleLogs").WithComparison(StringComparison.CurrentCulture);
    }

    [Test]
    public async Task ConsoleLogsAssembly_DoesNotReferenceStructuredLogsOrExternalProviders()
    {
        var references = typeof(ConsoleLogsFeature)
            .Assembly
            .GetReferencedAssemblies()
            .Select(x => x.Name)
            .ToList();

        await Assert.That(references).DoesNotContain("Elsa.Diagnostics.StructuredLogs");
        await Assert.That(references).DoesNotContain("Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite");
        await Assert.That(references).DoesNotContain("ConsoleLogStreaming.Persistence.Sqlite");
    }
}
