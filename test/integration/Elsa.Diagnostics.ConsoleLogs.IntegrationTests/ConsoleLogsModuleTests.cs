using Elsa.Diagnostics.ConsoleLogs.Extensions;
using Elsa.Diagnostics.ConsoleLogs.Features;
using Elsa.Diagnostics.ConsoleLogs.Permissions;

namespace Elsa.Diagnostics.ConsoleLogs.IntegrationTests;

public class ConsoleLogsModuleTests
{
    [Fact]
    public void Module_UsesDiagnosticsConsoleLogsIdentity()
    {
        Assert.Equal("/elsa/hubs/diagnostics/console-logs", EndpointRouteBuilderExtensions.HubRoute);
        Assert.Equal("read:diagnostics:console-logs", ConsoleLogsPermissions.Read);
        Assert.StartsWith("Elsa.Diagnostics.ConsoleLogs", typeof(ConsoleLogsFeature).Namespace);
    }

    [Fact]
    public void ConsoleLogsAssembly_DoesNotReferenceStructuredLogsOrExternalProviders()
    {
        var references = typeof(ConsoleLogsFeature)
            .Assembly
            .GetReferencedAssemblies()
            .Select(x => x.Name)
            .ToList();

        Assert.DoesNotContain("Elsa.Diagnostics.StructuredLogs", references);
        Assert.DoesNotContain("Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite", references);
        Assert.DoesNotContain("ConsoleLogStreaming.Persistence.Sqlite", references);
    }
}
