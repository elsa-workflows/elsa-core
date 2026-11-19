using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Scheduling.Quartz.Features;
using Elsa.Scheduling.Quartz.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Quartz;
using Quartz.Simpl;
using Quartz.Spi;

namespace Elsa.Scheduling.Quartz.UnitTests.Features;

public class QuartzFeatureRegistrationTests
{
    [Fact]
    public void Apply_RegistersTenantAwareJobFactory()
    {
        var services = new ServiceCollection();
        var module = new Mock<IModule>();
        module.Setup(m => m.Services).Returns(services);

        new QuartzFeature(module.Object).Apply();

        AssertSingleTenantAwareFactory(services);
    }

    [Fact]
    public void SchedulerFeature_Apply_RegistersTenantAwareJobFactory()
    {
        var services = new ServiceCollection();
        var module = new Mock<IModule>();
        module.Setup(m => m.Services).Returns(services);

        new QuartzSchedulerFeature(module.Object).Apply();

        AssertSingleTenantAwareFactory(services);
    }

    [Fact]
    public void BackgroundActivitySchedulerFeature_Apply_RegistersTenantAwareJobFactory()
    {
        var services = new ServiceCollection();
        var module = new Mock<IModule>();
        module.Setup(m => m.Services).Returns(services);

        new QuartzBackgroundActivitySchedulerFeature(module.Object).Apply();

        AssertSingleTenantAwareFactory(services);
    }

    [Fact]
    public void Apply_QuartzThenScheduler_RegistersFactoryOnce()
    {
        var services = new ServiceCollection();
        var module = new Mock<IModule>();
        module.Setup(m => m.Services).Returns(services);

        new QuartzFeature(module.Object).Apply();
        new QuartzSchedulerFeature(module.Object).Apply();
        new QuartzBackgroundActivitySchedulerFeature(module.Object).Apply();

        AssertSingleTenantAwareFactory(services);
    }

    [Fact]
    public void TryUse_ReplacesQuartzDefaultFactory()
    {
        var services = new ServiceCollection();
        services.AddQuartz();

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IJobFactory) &&
            descriptor.ImplementationType == typeof(MicrosoftDependencyInjectionJobFactory));

        JobFactoryRegistration.TryUseTenantAwareJobFactory(services);

        AssertSingleTenantAwareFactory(services);
    }

    [Fact]
    public void TryUse_KeepsHostTypedJobFactory()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IJobFactory, HostJobFactory>();

        JobFactoryRegistration.TryUseTenantAwareJobFactory(services);
        new QuartzSchedulerFeature(MockModule(services)).Apply();

        var factory = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IJobFactory));
        Assert.Equal(typeof(HostJobFactory), factory.ImplementationType);
    }

    [Fact]
    public void TryUse_KeepsHostFactoryRegisteredAfterAddQuartz()
    {
        var services = new ServiceCollection();
        services.AddQuartz();
        services.AddSingleton<IJobFactory, HostJobFactory>();

        JobFactoryRegistration.TryUseTenantAwareJobFactory(services);

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IJobFactory) &&
            descriptor.ImplementationType == typeof(HostJobFactory));
        Assert.DoesNotContain(services, descriptor =>
            descriptor.ServiceType == typeof(IJobFactory) &&
            descriptor.ImplementationType == typeof(TenantAwareJobFactory));
    }

    [Fact]
    public void TryUse_KeepsHostFactoryDelegate()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IJobFactory>(_ => new HostJobFactory());

        JobFactoryRegistration.TryUseTenantAwareJobFactory(services);

        var factory = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IJobFactory));
        Assert.NotNull(factory.ImplementationFactory);
        Assert.Null(factory.ImplementationType);
    }

    [Fact]
    public void TryUse_IsIdempotentAcrossFeatureOrder()
    {
        var services = new ServiceCollection();
        JobFactoryRegistration.TryUseTenantAwareJobFactory(services);
        services.AddQuartz();
        JobFactoryRegistration.TryUseTenantAwareJobFactory(services);

        AssertSingleTenantAwareFactory(services);
    }

    private static IModule MockModule(IServiceCollection services)
    {
        var module = new Mock<IModule>();
        module.Setup(m => m.Services).Returns(services);
        return module.Object;
    }

    private static void AssertSingleTenantAwareFactory(IServiceCollection services)
    {
        var factory = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IJobFactory));
        Assert.Equal(typeof(TenantAwareJobFactory), factory.ImplementationType);
    }

    private sealed class HostJobFactory : IJobFactory
    {
        public IJob NewJob(TriggerFiredBundle bundle, global::Quartz.IScheduler scheduler) => throw new NotSupportedException();
        public void ReturnJob(IJob job)
        {
        }
    }
}
