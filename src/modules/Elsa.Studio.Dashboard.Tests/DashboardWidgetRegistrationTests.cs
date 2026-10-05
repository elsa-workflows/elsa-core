using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Studio.Attributes;
using Elsa.Studio.Authorization;
using Elsa.Studio.Contracts;
using Elsa.Studio.Dashboard.Extensions;
using Elsa.Studio.Dashboard.Widgets;
using Elsa.Studio.Diagnostics.ConsoleLogs.Dashboard.Extensions;
using Elsa.Studio.Diagnostics.ConsoleLogs.Dashboard.UI.Dashboard;
using Elsa.Studio.Diagnostics.OpenTelemetry.Dashboard.Extensions;
using Elsa.Studio.Diagnostics.OpenTelemetry.Dashboard.UI.Dashboard;
using Elsa.Studio.Diagnostics.StructuredLogs.Dashboard.Extensions;
using Elsa.Studio.Diagnostics.StructuredLogs.Dashboard.UI.Dashboard;
using Elsa.Studio.Services;
using Elsa.Studio.Testing;
using Elsa.Studio.Workflows;
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
        Assert.Empty(descriptor.RequiredPermissions);
    }

    [Fact]
    public void AWidgetDeclaringNoPermission_IsPermittedToEveryone()
    {
        var descriptor = new DashboardWidgetDescriptor("test", DashboardWidgetZones.Metrics, 10, typeof(TestWidget));

        Assert.Empty(descriptor.RequiredPermissions);
        Assert.True(descriptor.IsPermitted(StubPermissionService.Grants("unrelated:view")));
    }

    // The grant matching itself (wildcards, any-of) is covered by UserPermissionsTests.
    [Theory]
    [InlineData("acme/things:view", true)]
    [InlineData("secrets:view", false)]
    [InlineData(null, true)]
    public void AWidget_IsPermittedToUsersHoldingAnyOfItsPermissions(string? grant, bool permitted)
    {
        var descriptor = new DashboardWidgetDescriptor("test", DashboardWidgetZones.Metrics, 10, typeof(TestWidget)) { RequiredPermissions = DashboardPermissions.ForData("acme/things") };

        Assert.Equal(permitted, descriptor.IsPermitted(grant == null ? UserPermissions.Unknown : StubPermissionService.Grants(grant)));
    }

    [Fact]
    public void AddDashboardWidget_WithPermissions_RegistersThemOnTheDescriptor()
    {
        var services = new ServiceCollection();

        services.AddDashboardWidget<TestWidget>("test", DashboardWidgetZones.PrimaryPanels, 20, [new("acme/things", PermissionVerbs.View)], "Test", "Capability", "Payload");
        var descriptor = services.BuildServiceProvider().GetRequiredService<DashboardWidgetDescriptor>();

        Assert.Equal("Test", descriptor.Title);
        Assert.Equal("Payload", descriptor.PayloadKind);
        Assert.Equal([new Permission("acme/things", PermissionVerbs.View)], descriptor.RequiredPermissions);
    }

    [Fact]
    public void TheDashboardsWorkflowPermissions_AreTheWorkflowModules()
    {
        Assert.Equal(WorkflowPermissions.Instances, DashboardPermissions.WorkflowInstances);
        Assert.Equal(WorkflowPermissions.Runtime, DashboardPermissions.WorkflowRuntime);
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
        var projectFile = FindRepositoryRoot().Combine("src/modules/Elsa.Studio.Dashboard/Elsa.Studio.Dashboard.csproj");
        var project = File.ReadAllText(projectFile);

        Assert.DoesNotContain("Elsa.Studio.Diagnostics", project);
        Assert.DoesNotContain("Elsa.Studio.Workflows", project);
    }

    [Fact]
    public void OwnerProjects_DoNotReferenceDashboardModule()
    {
        var root = FindRepositoryRoot();
        var ownerProjects = new[]
        {
            "src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/Elsa.Studio.Diagnostics.ConsoleLogs.csproj",
            "src/modules/Elsa.Studio.Diagnostics.StructuredLogs/Elsa.Studio.Diagnostics.StructuredLogs.csproj",
            "src/modules/Elsa.Studio.Workflows/Elsa.Studio.Workflows.csproj"
        };

        foreach (var ownerProject in ownerProjects)
        {
            var project = File.ReadAllText(root.Combine(ownerProject));
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

    // Each widget is visible with the view permission of the data it shows, or with dashboard:view where the dashboard
    // API serves that data.
    [Theory]
    [InlineData("dashboard.workflow.metrics", "dashboard:view,workflows/instances:view")]
    [InlineData("dashboard.needs-attention", "dashboard:view,workflows/instances:view")]
    [InlineData("dashboard.workflow.trend", "dashboard:view,workflows/instances:view")]
    [InlineData("dashboard.workflow.recent-activity", "dashboard:view,workflows/instances:view")]
    [InlineData("dashboard.workflow.hotspots", "dashboard:view,workflows/instances:view")]
    [InlineData("diagnostics.structured-logs", "dashboard:view,diagnostics/structured-logs:view")]
    [InlineData("diagnostics.console-logs", "dashboard:view,diagnostics/console-logs:view")]
    [InlineData("diagnostics.open-telemetry", "diagnostics/opentelemetry:view")]
    public async Task EachBuiltInWidget_DeclaresThePermissionsOfItsData(string id, string permissions)
    {
        var services = new ServiceCollection();

        services
            .AddDashboardModule()
            .AddWorkflowsDashboardModule()
            .AddStructuredLogsDashboardModule()
            .AddConsoleLogsDashboardModule()
            .AddOpenTelemetryDashboardModule();

        var serviceProvider = services.BuildServiceProvider();

        foreach (var feature in serviceProvider.GetRequiredService<IEnumerable<IFeature>>().Where(x => x.GetType().Namespace?.EndsWith(".Dashboard", StringComparison.Ordinal) == true))
            await feature.InitializeAsync();

        var descriptor = Assert.Single(serviceProvider.GetRequiredService<IDashboardWidgetRegistry>().List(), x => x.Id == id);

        Assert.Equal(permissions.Split(','), descriptor.RequiredPermissions.Select(x => x.ToString()));
    }

    [Fact]
    public void DashboardCompanionFeatures_DeclareRemoteBackendFeatureNames()
    {
        AssertRemoteFeatureName<Elsa.Studio.Workflows.Dashboard.Feature>("Elsa.Workflows.Runtime.Dashboard.ShellFeatures.WorkflowRuntimeDashboard");
        AssertRemoteFeatureName<Elsa.Studio.Diagnostics.StructuredLogs.Dashboard.Feature>("Elsa.Diagnostics.StructuredLogs.Dashboard.ShellFeatures.StructuredLogsDashboard");
        AssertRemoteFeatureName<Elsa.Studio.Diagnostics.ConsoleLogs.Dashboard.Feature>("Elsa.Diagnostics.ConsoleLogs.Dashboard.ShellFeatures.ConsoleLogsDashboard");
        AssertRemoteFeatureName<Elsa.Studio.Diagnostics.OpenTelemetry.Dashboard.Feature>("Elsa.Diagnostics.OpenTelemetry.ShellFeatures.OpenTelemetry");
    }

    [Fact]
    public async Task DefaultFeatureService_RegistersWidgetsWhenStaticCatalogAdvertisesShortNames()
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
        var featureService = new DefaultFeatureService(
            features,
            new CatalogRemoteFeatureProvider(
                new FeatureDescriptor { FullName = "Elsa.WorkflowRuntimeDashboard" },
                new FeatureDescriptor { FullName = "Elsa.StructuredLogsDashboard" },
                new FeatureDescriptor { FullName = "Elsa.ConsoleLogsDashboard" },
                new FeatureDescriptor { FullName = "Elsa.OpenTelemetry" }));

        await featureService.InitializeFeaturesAsync();

        var descriptors = registry.List();

        AssertDescriptor<DashboardWorkflowMetricsWidget>(descriptors, "dashboard.workflow.metrics", DashboardWidgetZones.Metrics, 100, "WorkflowInstances");
        AssertDescriptor<StructuredLogsDashboardWidget>(descriptors, "diagnostics.structured-logs", DashboardWidgetZones.DiagnosticsStatus, 100, "Diagnostics.StructuredLogs");
        AssertDescriptor<ConsoleLogsDashboardWidget>(descriptors, "diagnostics.console-logs", DashboardWidgetZones.DiagnosticsStatus, 200, "Diagnostics.ConsoleLogs");
        AssertDescriptor<OpenTelemetryDashboardWidget>(descriptors, "diagnostics.open-telemetry", DashboardWidgetZones.DiagnosticsStatus, 300, "OpenTelemetry.StorageDiagnostics");
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

    private sealed class CatalogRemoteFeatureProvider(params FeatureDescriptor[] features) : IRemoteFeatureProvider
    {
        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) =>
            Task.FromResult(features.Any(feature => string.Equals(feature.FullName, featureName, StringComparison.Ordinal)));

        public Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<FeatureDescriptor>>(features);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Elsa.Studio.sln")))
                return directory;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}

internal static class DirectoryInfoExtensions
{
    public static string Combine(this DirectoryInfo directory, string path) => Path.Combine(directory.FullName, path);
}
