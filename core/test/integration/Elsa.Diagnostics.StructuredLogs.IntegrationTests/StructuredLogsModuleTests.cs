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
    [Fact]
    public void StructuredLogsFeature_BelongsToStructuredLogsAssembly()
    {
        Assert.Equal("Elsa.Diagnostics.StructuredLogs", typeof(StructuredLogsFeature).Assembly.GetName().Name);
    }

    [Fact]
    public void ShellStructuredLogsFeature_BelongsToStructuredLogsAssembly()
    {
        Assert.Equal("Elsa.Diagnostics.StructuredLogs", typeof(ShellStructuredLogsFeature).Assembly.GetName().Name);
    }

    [Fact]
    public void ShellStructuredLogsFeature_UsesDiagnosticsStructuredLogsFeatureName()
    {
        Assert.Equal("Elsa.Diagnostics.StructuredLogs.ShellFeatures.StructuredLogsFeature", typeof(ShellStructuredLogsFeature).FullName);
    }

    [Fact]
    public void Readme_SeparatesStructuredLogsFromFutureDiagnosticsModules()
    {
        var readme = File.ReadAllText(FindReadme());

        Assert.Contains("semantic `ILogger` records only", readme);
        Assert.Contains("future diagnostics console logs module", readme);
        Assert.Contains("future diagnostics OpenTelemetry module", readme);
    }

    [Fact]
    public void ShellStructuredLogsFeature_RegistersFastEndpointsAndWebEndpointMapping()
    {
        Assert.True(typeof(IFastEndpointsShellFeature).IsAssignableFrom(typeof(ShellStructuredLogsFeature)));
        Assert.True(typeof(IWebShellFeature).IsAssignableFrom(typeof(ShellStructuredLogsFeature)));
    }

    [Fact]
    public void ShellStructuredLogsFeature_CopiesBindablePropertiesToOptions()
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

        var options = services.BuildServiceProvider().GetRequiredService<IOptions<StructuredLogsOptions>>().Value;
        Assert.Equal(feature.RecentLogCapacity, options.RecentLogCapacity);
        Assert.Equal(feature.SubscriberChannelCapacity, options.SubscriberChannelCapacity);
        Assert.Equal(feature.MaxRecentLogQuerySize, options.MaxRecentLogQuerySize);
        Assert.Equal(feature.SourceHeartbeatTimeout, options.SourceHeartbeatTimeout);
        Assert.Equal(feature.IncludeStructuredLogsInternalLogs, options.IncludeStructuredLogsInternalLogs);
        Assert.Equal(feature.SensitiveNames, options.SensitiveNames);
        Assert.Equal(feature.SensitiveTextPatterns, options.SensitiveTextPatterns);
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
