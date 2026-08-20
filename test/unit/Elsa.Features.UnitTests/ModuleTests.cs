using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Contracts;
using Elsa.Features.Implementations;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Features.UnitTests;

/// <summary>
/// Tests <see cref="Module.Apply"/>, with an emphasis on features that introduce additional features from their own <see cref="IFeature.Apply"/> method.
/// </summary>
public class ModuleTests
{
    private const string AppliedFeaturesKey = "AppliedFeatures";

    private readonly ServiceCollection _services = new();
    private readonly Module _module;
    private readonly List<Type> _appliedFeatures = new();

    public ModuleTests()
    {
        _module = new(_services);
        _module.Properties[AppliedFeaturesKey] = _appliedFeatures;
    }

    [Fact]
    public void Apply_AppliesFeatureIntroducedFromApply()
    {
        _module.Configure<IntroducingFeature>();

        _module.Apply();

        Assert.Contains(typeof(IntroducedFeature), _appliedFeatures);
        Assert.Contains(_services, x => x.ServiceType == typeof(IntroducedMarker));
    }

    [Fact]
    public void Apply_AppliesEntireChainOfFeaturesIntroducedFromApply()
    {
        _module.Configure<ChainIntroducingFeature>();

        _module.Apply();

        Assert.Contains(typeof(ChainMiddleFeature), _appliedFeatures);
        Assert.Contains(typeof(ChainLeafFeature), _appliedFeatures);
    }

    [Fact]
    public void Apply_AppliesDependenciesOfFeatureIntroducedFromApplyBeforeThatFeature()
    {
        _module.Configure<IntroducingDependentFeature>();

        _module.Apply();

        Assert.Contains(typeof(IntroducedDependencyFeature), _appliedFeatures);
        Assert.True(_appliedFeatures.IndexOf(typeof(IntroducedDependencyFeature)) < _appliedFeatures.IndexOf(typeof(IntroducedDependentFeature)));
    }

    [Fact]
    public void Apply_RegistersHostedServicesOfFeatureIntroducedFromApply()
    {
        _module.Configure<IntroducingFeature>();

        _module.Apply();

        Assert.Contains(_services, x => x.ServiceType == typeof(IHostedService) && x.ImplementationType == typeof(IntroducedHostedService));
    }

    [Fact]
    public void Apply_ListsFeatureIntroducedFromApplyInTheInstalledFeatureRegistry()
    {
        _module.Configure<IntroducingFeature>();

        _module.Apply();

        Assert.NotNull(GetInstalledFeatureRegistry().Find("Elsa.Introduced"));
    }

    [Fact]
    public void Apply_AppliesEachFeatureOnlyOnce()
    {
        _module.Configure<IntroducingFeature>();

        _module.Apply();

        Assert.Equal(_appliedFeatures.Distinct().Count(), _appliedFeatures.Count);
    }

    [Fact]
    public void Apply_AppliesFeaturesInDependencyOrder()
    {
        _module.Configure<DependentFeature>();

        _module.Apply();

        Assert.Equal([typeof(DependencyFeature), typeof(DependentFeature)], _appliedFeatures);
    }

    [Fact]
    public void Apply_RegistersHostedServicesInPriorityOrder()
    {
        _module.ConfigureHostedService<SecondHostedService>(2);
        _module.ConfigureHostedService<FirstHostedService>(1);
        _module.Configure<IntroducingFeature>();

        _module.Apply();

        var hostedServiceTypes = _services.Where(x => x.ServiceType == typeof(IHostedService)).Select(x => x.ImplementationType).ToList();
        Assert.Equal([typeof(FirstHostedService), typeof(SecondHostedService), typeof(IntroducedHostedService)], hostedServiceTypes);
    }

    private IInstalledFeatureRegistry GetInstalledFeatureRegistry()
    {
        return (IInstalledFeatureRegistry)_services.Single(x => x.ServiceType == typeof(IInstalledFeatureRegistry)).ImplementationInstance!;
    }

    /// <summary>
    /// Records the order in which features are applied so that tests can assert on it.
    /// </summary>
    public abstract class RecordingFeature(IModule module) : FeatureBase(module)
    {
        public override void Apply() => ((List<Type>)Module.Properties[AppliedFeaturesKey]).Add(GetType());
    }

    public class IntroducingFeature(IModule module) : RecordingFeature(module)
    {
        public override void Apply()
        {
            base.Apply();
            Module.Configure<IntroducedFeature>();
        }
    }

    public class IntroducedFeature(IModule module) : RecordingFeature(module)
    {
        public override void ConfigureHostedServices() => ConfigureHostedService<IntroducedHostedService>(3);

        public override void Apply()
        {
            base.Apply();
            Services.AddSingleton<IntroducedMarker>();
        }
    }

    public class ChainIntroducingFeature(IModule module) : RecordingFeature(module)
    {
        public override void Apply()
        {
            base.Apply();
            Module.Configure<ChainMiddleFeature>();
        }
    }

    public class ChainMiddleFeature(IModule module) : RecordingFeature(module)
    {
        public override void Apply()
        {
            base.Apply();
            Module.Configure<ChainLeafFeature>();
        }
    }

    public class ChainLeafFeature(IModule module) : RecordingFeature(module);

    public class IntroducingDependentFeature(IModule module) : RecordingFeature(module)
    {
        public override void Apply()
        {
            base.Apply();
            Module.Configure<IntroducedDependentFeature>();
        }
    }

    [DependsOn(typeof(IntroducedDependencyFeature))]
    public class IntroducedDependentFeature(IModule module) : RecordingFeature(module);

    public class IntroducedDependencyFeature(IModule module) : RecordingFeature(module);

    [DependsOn(typeof(DependencyFeature))]
    public class DependentFeature(IModule module) : RecordingFeature(module);

    public class DependencyFeature(IModule module) : RecordingFeature(module);

    public class IntroducedMarker;

    public abstract class NoopHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public class IntroducedHostedService : NoopHostedService;

    public class FirstHostedService : NoopHostedService;

    public class SecondHostedService : NoopHostedService;
}
