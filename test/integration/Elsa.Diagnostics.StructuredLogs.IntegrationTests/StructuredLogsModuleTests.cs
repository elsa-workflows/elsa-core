using Elsa.Diagnostics.StructuredLogs.Features;
using CShells.AspNetCore.Features;
using CShells.FastEndpoints.Features;
using Elsa.Diagnostics.StructuredLogs.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShellStructuredLogsFeature = Elsa.Diagnostics.StructuredLogs.ShellFeatures.StructuredLogsFeature;

namespace Elsa.Diagnostics.StructuredLogs.IntegrationTests;

public class StructuredLogsModuleTests
{
    [Test]
    public async Task StructuredLogsFeature_BelongsToStructuredLogsAssembly()
    {
        await Assert.That(typeof(StructuredLogsFeature).Assembly.GetName().Name).IsEqualTo("Elsa.Diagnostics.StructuredLogs");
    }

    [Test]
    public async Task ShellStructuredLogsFeature_BelongsToStructuredLogsAssembly()
    {
        await Assert.That(typeof(ShellStructuredLogsFeature).Assembly.GetName().Name).IsEqualTo("Elsa.Diagnostics.StructuredLogs");
    }

    [Test]
    public async Task ShellStructuredLogsFeature_UsesDiagnosticsStructuredLogsFeatureName()
    {
        await Assert.That(typeof(ShellStructuredLogsFeature).FullName).IsEqualTo("Elsa.Diagnostics.StructuredLogs.ShellFeatures.StructuredLogsFeature");
    }

    [Test]
    public async Task Readme_SeparatesStructuredLogsFromFutureDiagnosticsModules()
    {
        var readme = File.ReadAllText(FindReadme());

        await Assert.That(readme).Contains("semantic `ILogger` records only").WithComparison(StringComparison.CurrentCulture);
        await Assert.That(readme).Contains("future diagnostics console logs module").WithComparison(StringComparison.CurrentCulture);
        await Assert.That(readme).Contains("future diagnostics OpenTelemetry module").WithComparison(StringComparison.CurrentCulture);
    }

    [Test]
    public async Task ShellStructuredLogsFeature_RegistersFastEndpointsAndWebEndpointMapping()
    {
        await Assert.That(typeof(IFastEndpointsShellFeature).IsAssignableFrom(typeof(ShellStructuredLogsFeature))).IsTrue();
        await Assert.That(typeof(IWebShellFeature).IsAssignableFrom(typeof(ShellStructuredLogsFeature))).IsTrue();
    }

    [Test]
    public async Task ShellStructuredLogsFeature_CopiesBindablePropertiesToOptions()
    {
        var feature = new ShellStructuredLogsFeature
        {
            RecentLogCapacity = 123,
            SubscriberChannelCapacity = 45,
            MaxRecentLogQuerySize = 67,
            SourceHeartbeatTimeout = TimeSpan.FromSeconds(89),
            IncludeStructuredLogsInternalLogs = true,
            SensitiveNames = ["credential"],
            SensitiveTextPatterns = ["(?i)credential=([^\\s]+)"]
        };
        var services = new ServiceCollection();

        feature.ConfigureServices(services);

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<StructuredLogsOptions>>().Value;
        await Assert.That(options.RecentLogCapacity).IsEqualTo(feature.RecentLogCapacity);
        await Assert.That(options.SubscriberChannelCapacity).IsEqualTo(feature.SubscriberChannelCapacity);
        await Assert.That(options.MaxRecentLogQuerySize).IsEqualTo(feature.MaxRecentLogQuerySize);
        await Assert.That(options.SourceHeartbeatTimeout).IsEqualTo(feature.SourceHeartbeatTimeout);
        await Assert.That(options.IncludeStructuredLogsInternalLogs).IsEqualTo(feature.IncludeStructuredLogsInternalLogs);
        await Assert.That(options.SensitiveNames).IsEquivalentTo(feature.SensitiveNames, TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(options.SensitiveTextPatterns).IsEquivalentTo(feature.SensitiveTextPatterns, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    private static string FindReadme()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "modules", "Elsa.Diagnostics.StructuredLogs", "README.md");
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not find the structured logs README.");
    }
}
