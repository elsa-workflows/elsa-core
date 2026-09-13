using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Contracts;
using Elsa.Features.Implementations;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

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

    [Test]
    public async Task Apply_AppliesFeatureIntroducedFromApply()
    {
        _module.Configure<IntroducingFeature>();

        _module.Apply();

        await Assert.That(_appliedFeatures).Contains(typeof(IntroducedFeature));
        await Assert.That(_services).Contains(x => x.ServiceType == typeof(IntroducedMarker));
    }

    [Test]
    public async Task Apply_AppliesEntireChainOfFeaturesIntroducedFromApply()
    {
        _module.Configure<ChainIntroducingFeature>();

        _module.Apply();

        await Assert.That(_appliedFeatures).Contains(typeof(ChainMiddleFeature));
        await Assert.That(_appliedFeatures).Contains(typeof(ChainLeafFeature));
    }

    [Test]
    public async Task Apply_AppliesDependenciesOfFeatureIntroducedFromApplyBeforeThatFeature()
    {
        _module.Configure<IntroducingDependentFeature>();

        _module.Apply();

        await Assert.That(_appliedFeatures).Contains(typeof(IntroducedDependencyFeature));
        await Assert.That(_appliedFeatures.IndexOf(typeof(IntroducedDependencyFeature)) < _appliedFeatures.IndexOf(typeof(IntroducedDependentFeature))).IsTrue();
    }

    [Test]
    public async Task Apply_RegistersHostedServicesOfFeatureIntroducedFromApply()
    {
        _module.Configure<IntroducingFeature>();

        _module.Apply();

        await Assert.That(_services).Contains(x => x.ServiceType == typeof(IHostedService) && x.ImplementationType == typeof(IntroducedHostedService));
    }

    [Test]
    public async Task Apply_ListsFeatureIntroducedFromApplyInTheInstalledFeatureRegistry()
    {
        _module.Configure<IntroducingFeature>();

        _module.Apply();

        await Assert.That(GetInstalledFeatureRegistry().Find("Elsa.Introduced")).IsNotNull();
    }

    [Test]
    public async Task Apply_AppliesEachFeatureOnlyOnce()
    {
        _module.Configure<IntroducingFeature>();

        _module.Apply();

        await Assert.That(_appliedFeatures.Count).IsEqualTo(_appliedFeatures.Distinct().Count());
    }

    [Test]
    public async Task Apply_AppliesFeaturesInDependencyOrder()
    {
        _module.Configure<DependentFeature>();

        _module.Apply();

        await Assert.That(_appliedFeatures).IsEquivalentTo(
            [typeof(DependencyFeature), typeof(DependentFeature)],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task Apply_RegistersHostedServicesInPriorityOrder()
    {
        _module.ConfigureHostedService<SecondHostedService>(2);
        _module.ConfigureHostedService<FirstHostedService>(1);
        _module.Configure<IntroducingFeature>();

        _module.Apply();

        await Assert.That(GetHostedServiceTypes()).IsEquivalentTo(
            [typeof(FirstHostedService), typeof(SecondHostedService), typeof(IntroducedHostedService)],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task Apply_OrdersHostedServiceOfFeatureIntroducedFromApplyByPriority()
    {
        _module.ConfigureHostedService<SecondHostedService>(10);
        _module.Configure<IntroducingFeature>();

        _module.Apply();

        // The introduced feature configures its hosted service at priority 3, so it has to come first even though it shows up last.
        await Assert.That(GetHostedServiceTypes()).IsEquivalentTo(
            [typeof(IntroducedHostedService), typeof(SecondHostedService)],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task Apply_RegistersConfiguredHostedServicesBeforeThoseRegisteredFromApply()
    {
        _module.ConfigureHostedService<FirstHostedService>();
        _module.Configure<HostedServiceRegisteringFeature>();

        _module.Apply();

        await Assert.That(GetHostedServiceTypes()).IsEquivalentTo(
            [typeof(FirstHostedService), typeof(SelfRegisteredHostedService)],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    private List<Type> GetHostedServiceTypes()
    {
        return _services.Where(x => x.ServiceType == typeof(IHostedService)).Select(x => x.ImplementationType!).ToList();
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

    /// <summary>
    /// Registers a hosted service straight with the service collection, the way <c>WorkflowRuntimeFeature</c> does, rather than through the module.
    /// </summary>
    public class HostedServiceRegisteringFeature(IModule module) : RecordingFeature(module)
    {
        public override void Apply()
        {
            base.Apply();
            Services.AddHostedService<SelfRegisteredHostedService>();
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

    [Elsa.Features.Attributes.DependsOn(typeof(IntroducedDependencyFeature))]
    public class IntroducedDependentFeature(IModule module) : RecordingFeature(module);

    public class IntroducedDependencyFeature(IModule module) : RecordingFeature(module);

    [Elsa.Features.Attributes.DependsOn(typeof(DependencyFeature))]
    public class DependentFeature(IModule module) : RecordingFeature(module);

    public class DependencyFeature(IModule module) : RecordingFeature(module);

    public class IntroducedMarker;

    public abstract class NoopHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public class IntroducedHostedService : NoopHostedService;

    public class SelfRegisteredHostedService : NoopHostedService;

    public class FirstHostedService : NoopHostedService;

    public class SecondHostedService : NoopHostedService;
}
