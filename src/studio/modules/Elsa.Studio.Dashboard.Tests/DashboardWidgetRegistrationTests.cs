using Elsa.Studio.Attributes;
using Elsa.Studio.Contracts;
using Elsa.Studio.Dashboard.Extensions;
using Elsa.Studio.Dashboard.Widgets;
using Elsa.Studio.Diagnostics.ConsoleLogs.Dashboard.Extensions;
using Elsa.Studio.Diagnostics.ConsoleLogs.Dashboard.UI.Dashboard;
using Elsa.Studio.Diagnostics.OpenTelemetry.Dashboard.Extensions;
using Elsa.Studio.Diagnostics.OpenTelemetry.Dashboard.UI.Dashboard;
using Elsa.Studio.Diagnostics.StructuredLogs.Dashboard.Extensions;
using Elsa.Studio.Diagnostics.StructuredLogs.Dashboard.UI.Dashboard;
using Elsa.Studio.Workflows.Dashboard.Extensions;
using Elsa.Studio.Workflows.Dashboard.Widgets;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Studio.Dashboard.Tests;

public class DashboardWidgetRegistrationTests
{
    [Fact]
    public void AddDashboardWidget_RegistersDescriptor()
    {
        var services = new ServiceCollection();

        services.AddDashboardWidget<TestWidget>("test", DashboardWidgetZones.PrimaryPanels, 20, "Test", "Capability", "Payload");
        var descriptor = services.BuildServiceProvider().GetRequiredService<DashboardWidgetDescriptor>();

        Assert.Equal("test", descriptor.Id);
        Assert.Equal(DashboardWidgetZones.PrimaryPanels, descriptor.Zone);
        Assert.Equal(20, descriptor.Order);
        Assert.Equal(typeof(TestWidget), descriptor.ComponentType);
        Assert.Equal("Capability", descriptor.RequiredBackendCapability);
        Assert.Equal("Payload", descriptor.PayloadKind);
    }

    [Fact]
    public void Descriptors_OrderDeterministicallyByOrderThenId()
    {
        var descriptors = new[]
        {
            new DashboardWidgetDescriptor("b", DashboardWidgetZones.Metrics, 10, typeof(TestWidget)),
            new DashboardWidgetDescriptor("a", DashboardWidgetZones.Metrics, 10, typeof(TestWidget)),
            new DashboardWidgetDescriptor("c", DashboardWidgetZones.Metrics, 5, typeof(TestWidget))
        };

        var ordered = descriptors.OrderBy(x => x.Order).ThenBy(x => x.Id, StringComparer.Ordinal).Select(x => x.Id).ToList();

        Assert.Equal(["c", "a", "b"], ordered);
    }

    [Fact]
    public void DashboardProject_DoesNotReferenceDiagnosticsModules()
    {
        var project = ReadRepositoryFile("src", "modules", "Elsa.Studio.Dashboard", "Elsa.Studio.Dashboard.csproj");

        Assert.DoesNotContain("Elsa.Studio.Diagnostics", project);
        Assert.DoesNotContain("Elsa.Studio.Workflows", project);
    }

    [Fact]
    public void OwnerProjects_DoNotReferenceDashboardModule()
    {
        var ownerProjects = new[]
        {
            "src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/Elsa.Studio.Diagnostics.ConsoleLogs.csproj",
            "src/modules/Elsa.Studio.Diagnostics.StructuredLogs/Elsa.Studio.Diagnostics.StructuredLogs.csproj",
            "src/modules/Elsa.Studio.Workflows/Elsa.Studio.Workflows.csproj"
        };

        foreach (var ownerProject in ownerProjects)
        {
            var project = ReadRepositoryFile(ownerProject.Split('/'));
            Assert.DoesNotContain("Elsa.Studio.Dashboard", project);
        }
    }

    [Fact]
    public async Task DashboardCompanionModules_RegisterExpectedWidgets()
    {
        var services = new ServiceCollection();

        services
            .AddDashboardModule()
            .AddWorkflowsDashboardModule()
            .AddStructuredLogsDashboardModule()
            .AddConsoleLogsDashboardModule()
            .AddOpenTelemetryDashboardModule();

        var serviceProvider = services.BuildServiceProvider();
        var features = serviceProvider.GetRequiredService<IEnumerable<IFeature>>().Where(x => x.GetType().Namespace?.EndsWith(".Dashboard", StringComparison.Ordinal) == true).ToList();
        var registry = serviceProvider.GetRequiredService<IDashboardWidgetRegistry>();

        Assert.Contains(features, x => x.GetType() == typeof(Elsa.Studio.Workflows.Dashboard.Feature));
        Assert.Contains(features, x => x.GetType() == typeof(Elsa.Studio.Diagnostics.StructuredLogs.Dashboard.Feature));
        Assert.Contains(features, x => x.GetType() == typeof(Elsa.Studio.Diagnostics.ConsoleLogs.Dashboard.Feature));
        Assert.Contains(features, x => x.GetType() == typeof(Elsa.Studio.Diagnostics.OpenTelemetry.Dashboard.Feature));

        foreach (var feature in features)
            await feature.InitializeAsync();

        var descriptors = registry.List();

        AssertDescriptor<DashboardWorkflowMetricsWidget>(descriptors, "dashboard.workflow.metrics", DashboardWidgetZones.Metrics, 100, "WorkflowInstances");
        AssertDescriptor<DashboardNeedsAttentionWidget>(descriptors, "dashboard.needs-attention", DashboardWidgetZones.Findings, 100, null);
        AssertDescriptor<DashboardTrendWidget>(descriptors, "dashboard.workflow.trend", DashboardWidgetZones.Trend, 100, "WorkflowTrends");
        AssertDescriptor<DashboardRecentActivityWidget>(descriptors, "dashboard.workflow.recent-activity", DashboardWidgetZones.Activity, 100, "RecentActivity");
        AssertDescriptor<DashboardWorkflowHotspotsWidget>(descriptors, "dashboard.workflow.hotspots", DashboardWidgetZones.SecondaryPanels, 100, "WorkflowHotspots");
        AssertDescriptor<StructuredLogsDashboardWidget>(descriptors, "diagnostics.structured-logs", DashboardWidgetZones.DiagnosticsStatus, 100, "Diagnostics.StructuredLogs");
        AssertDescriptor<ConsoleLogsDashboardWidget>(descriptors, "diagnostics.console-logs", DashboardWidgetZones.DiagnosticsStatus, 200, "Diagnostics.ConsoleLogs");
        AssertDescriptor<OpenTelemetryDashboardWidget>(descriptors, "diagnostics.open-telemetry", DashboardWidgetZones.DiagnosticsStatus, 300, "OpenTelemetry.StorageDiagnostics");
        Assert.Equal(8, descriptors.Count);
    }

    [Fact]
    public void DashboardCompanionFeatures_DeclareRemoteBackendFeatureNames()
    {
        AssertRemoteFeatureName<Elsa.Studio.Workflows.Dashboard.Feature>("Elsa.Workflows.Runtime.Dashboard.ShellFeatures.WorkflowRuntimeDashboard");
        AssertRemoteFeatureName<Elsa.Studio.Diagnostics.StructuredLogs.Dashboard.Feature>("Elsa.Diagnostics.StructuredLogs.Dashboard.ShellFeatures.StructuredLogsDashboard");
        AssertRemoteFeatureName<Elsa.Studio.Diagnostics.ConsoleLogs.Dashboard.Feature>("Elsa.Diagnostics.ConsoleLogs.Dashboard.ShellFeatures.ConsoleLogsDashboard");
        AssertRemoteFeatureName<Elsa.Studio.Diagnostics.OpenTelemetry.Dashboard.Feature>("Elsa.Diagnostics.OpenTelemetry.ShellFeatures.OpenTelemetry");
    }

    private sealed class TestWidget : IComponent
    {
        public void Attach(RenderHandle renderHandle)
        {
        }

        public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
    }

    private static void AssertDescriptor<TComponent>(
        IReadOnlyCollection<DashboardWidgetDescriptor> descriptors,
        string id,
        string zone,
        int order,
        string? payloadKind)
    {
        var descriptor = Assert.Single(descriptors, x => x.Id == id);

        Assert.Equal(zone, descriptor.Zone);
        Assert.Equal(order, descriptor.Order);
        Assert.Equal(typeof(TComponent), descriptor.ComponentType);
        Assert.Equal(payloadKind, descriptor.PayloadKind);
    }

    private static void AssertRemoteFeatureName<TFeature>(string expectedName)
    {
        var attribute = typeof(TFeature).GetCustomAttributes(typeof(RemoteFeatureAttribute), false).OfType<RemoteFeatureAttribute>().Single();

        Assert.Equal(expectedName, attribute.Name);
    }

    private static string ReadRepositoryFile(params string[] pathSegments)
    {
        if (pathSegments.Length < 2 || pathSegments[0] != "src")
            throw new ArgumentException("Expected a Studio source path beginning with src.", nameof(pathSegments));

        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
        {
            var standalonePath = Path.Combine([current.FullName, .. pathSegments]);
            if (File.Exists(standalonePath))
                return File.ReadAllText(standalonePath);

            var consolidatedPath = Path.Combine([current.FullName, "src", "studio", .. pathSegments[1..]]);
            if (File.Exists(consolidatedPath))
                return File.ReadAllText(consolidatedPath);
        }

        throw new FileNotFoundException($"Could not locate Studio source file '{Path.Combine(pathSegments)}'.");
    }
}
