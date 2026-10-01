using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Studio.Attributes;
using Elsa.Studio.Contracts;
using Elsa.Studio.Services;
using Xunit;

namespace Elsa.Studio.Core.Tests;

public class DefaultFeatureServiceTests
{
    [Fact]
    public async Task InitializeFeatures_EnablesCompanionsAdvertisedByStaticCatalogNames()
    {
        var dashboard = new WorkflowRuntimeDashboardFeature();
        var structuredLogs = new StructuredLogsDashboardFeature();
        var service = new DefaultFeatureService(
            [dashboard, structuredLogs],
            new CatalogRemoteFeatureProvider(
                new FeatureDescriptor { FullName = "Elsa.WorkflowRuntimeDashboard" },
                new FeatureDescriptor { FullName = "Elsa.StructuredLogsDashboard" }));

        await service.InitializeFeaturesAsync();

        Assert.True(dashboard.Initialized);
        Assert.True(structuredLogs.Initialized);
    }

    [Fact]
    public async Task InitializeFeatures_EnablesCompanionsAdvertisedByShellFeatureNames()
    {
        var dashboard = new WorkflowRuntimeDashboardFeature();
        var service = new DefaultFeatureService(
            [dashboard],
            new CatalogRemoteFeatureProvider(
                new FeatureDescriptor { FullName = WorkflowRuntimeDashboardFeature.RemoteFeatureName }));

        await service.InitializeFeaturesAsync();

        Assert.True(dashboard.Initialized);
    }

    [Fact]
    public async Task InitializeFeatures_SkipsCompanionsMissingFromCatalog()
    {
        var dashboard = new WorkflowRuntimeDashboardFeature();
        var service = new DefaultFeatureService(
            [dashboard],
            new CatalogRemoteFeatureProvider(new FeatureDescriptor { FullName = "Elsa.Secrets" }));

        await service.InitializeFeaturesAsync();

        Assert.False(dashboard.Initialized);
    }

    [Fact]
    public async Task InitializeFeatures_SkipsCompanionsWhenCatalogLastSegmentIsNotElsaPrefixed()
    {
        var dashboard = new WorkflowRuntimeDashboardFeature();
        var identity = new IdentityFeature();
        var service = new DefaultFeatureService(
            [dashboard, identity],
            new CatalogRemoteFeatureProvider(
                new FeatureDescriptor { FullName = "Acme.WorkflowRuntimeDashboard" },
                new FeatureDescriptor { Name = "WorkflowRuntimeDashboard", Namespace = "Acme" },
                new FeatureDescriptor { FullName = "Acme.Identity" },
                new FeatureDescriptor { Name = "Identity", Namespace = "Acme" }));

        await service.InitializeFeaturesAsync();

        Assert.False(dashboard.Initialized);
        Assert.False(identity.Initialized);
    }

    [Fact]
    public async Task InitializeFeatures_AlwaysInitializesUngatedFeatures()
    {
        var local = new LocalFeature();
        var service = new DefaultFeatureService(
            [local],
            new CatalogRemoteFeatureProvider());

        await service.InitializeFeaturesAsync();

        Assert.True(local.Initialized);
    }

    [Fact]
    public async Task InitializeFeatures_ReportsItIsInitialized_ByTheTimeInitializedIsRaised()
    {
        IFeatureService service = new DefaultFeatureService([new LocalFeature()], new CatalogRemoteFeatureProvider());
        var initializedWhenRaised = false;
        service.Initialized += () => initializedWhenRaised = service.IsInitialized;
        Assert.False(service.IsInitialized);

        await service.InitializeFeaturesAsync();

        Assert.True(initializedWhenRaised);
        Assert.True(service.IsInitialized);
    }

    [RemoteFeature(RemoteFeatureName)]
    private sealed class WorkflowRuntimeDashboardFeature : TrackingFeature
    {
        public const string RemoteFeatureName = "Elsa.Workflows.Runtime.Dashboard.ShellFeatures.WorkflowRuntimeDashboard";
    }

    [RemoteFeature("Elsa.Diagnostics.StructuredLogs.Dashboard.ShellFeatures.StructuredLogsDashboard")]
    private sealed class StructuredLogsDashboardFeature : TrackingFeature;

    [RemoteFeature("Elsa.Identity.ShellFeatures.Identity")]
    private sealed class IdentityFeature : TrackingFeature;

    private sealed class LocalFeature : TrackingFeature;

    private abstract class TrackingFeature : IFeature
    {
        public bool Initialized { get; private set; }

        public ValueTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            Initialized = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CatalogRemoteFeatureProvider(params FeatureDescriptor[] features) : IRemoteFeatureProvider
    {
        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) =>
            Task.FromResult(features.Any(feature => string.Equals(feature.FullName, featureName, StringComparison.Ordinal)));

        public Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<FeatureDescriptor>>(features);
    }
}
