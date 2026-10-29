using Elsa.Features.Services;
using Elsa.Scheduling.Quartz.Features;
using Elsa.Scheduling.Quartz.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
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

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IJobFactory) &&
            descriptor.ImplementationType == typeof(TenantAwareJobFactory));
    }

    [Fact]
    public void SchedulerFeature_Apply_DoesNotRegisterTenantAwareJobFactory()
    {
        var services = new ServiceCollection();
        var module = new Mock<IModule>();
        module.Setup(m => m.Services).Returns(services);

        new QuartzSchedulerFeature(module.Object).Apply();

        Assert.DoesNotContain(services, descriptor =>
            descriptor.ServiceType == typeof(IJobFactory) &&
            descriptor.ImplementationType == typeof(TenantAwareJobFactory));
    }
}
