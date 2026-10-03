using Elsa.Studio.Contracts;
using Elsa.Studio.Services;
using Xunit;

namespace Elsa.Studio.Core.Tests;

/// <summary>
/// <see cref="IFeatureService.IsInitialized"/> stays a default interface member so third-party
/// implementations compiled against earlier Studio packages keep working. The stock service reports
/// initialized only after <see cref="IFeatureService.InitializeFeaturesAsync"/> completes.
/// </summary>
public class DefaultFeatureServiceTests
{
    [Fact]
    public async Task InitializeFeatures_ReportsItIsInitialized_ByTheTimeInitializedIsRaised()
    {
        IFeatureService service = new DefaultFeatureService([new LocalFeature()], new EmptyRemoteFeatureProvider());
        var initializedWhenRaised = false;
        service.Initialized += () => initializedWhenRaised = service.IsInitialized;
        Assert.False(service.IsInitialized);

        await service.InitializeFeaturesAsync();

        Assert.True(initializedWhenRaised);
        Assert.True(service.IsInitialized);
    }

    [Fact]
    public void UnimplementedIsInitialized_DefaultsToFalse()
    {
        IFeatureService service = new LegacyFeatureService();

        Assert.False(service.IsInitialized);
    }

    private sealed class LocalFeature : IFeature
    {
        public ValueTask InitializeAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    private sealed class EmptyRemoteFeatureProvider : IRemoteFeatureProvider
    {
        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IEnumerable<Elsa.Api.Client.Resources.Features.Models.FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<Elsa.Api.Client.Resources.Features.Models.FeatureDescriptor>>([]);
    }

    /// <summary>Simulates a third-party implementation compiled against the default interface member.</summary>
    private sealed class LegacyFeatureService : IFeatureService
    {
        public event Action? Initialized
        {
            add { }
            remove { }
        }

        public IEnumerable<IFeature> GetFeatures() => [];
        public Task InitializeFeaturesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
