using Elsa.Features.Services;
using Elsa.Scheduling.Features;
using Elsa.Scheduling.Hangfire.Features;
using Elsa.Scheduling.Hangfire.Handlers;
using Elsa.Scheduling.Hangfire.Services;
using Elsa.Workflows;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Elsa.Scheduling.Hangfire.UnitTests.Features;

public class HangfireSchedulerFeatureTests
{
    [Fact]
    public void Configure_SetsSchedulingWorkflowSchedulerFactory()
    {
        var module = new Mock<IModule>();
        var scheduling = new SchedulingFeature(module.Object);
        module.Setup(m => m.Configure(It.IsAny<Action<SchedulingFeature>>()))
            .Callback<Action<SchedulingFeature>?>(configure => configure?.Invoke(scheduling))
            .Returns(scheduling);

        var feature = new HangfireSchedulerFeature(module.Object);
        feature.Configure();

        var storage = new MemoryStorage();
        var expected = new HangfireWorkflowScheduler(
            new BackgroundJobClient(storage),
            new RecurringJobManager(storage),
            Mock.Of<Elsa.Common.Multitenancy.ITenantAccessor>(),
            storage);
        var services = new ServiceCollection();
        services.AddSingleton(expected);
        using var provider = services.BuildServiceProvider();

        var resolved = scheduling.WorkflowScheduler(provider);

        Assert.Same(expected, resolved);
    }

    [Fact]
    public void Apply_RegistersHangfireWorkflowSchedulerAndCronModifier()
    {
        var services = new ServiceCollection();
        var module = new Mock<IModule>();
        module.Setup(m => m.Services).Returns(services);

        var feature = new HangfireSchedulerFeature(module.Object);
        feature.Apply();

        Assert.Contains(services, d =>
            d.ServiceType == typeof(HangfireWorkflowScheduler) &&
            d.Lifetime == ServiceLifetime.Singleton);
        Assert.Contains(services, d =>
            d.ServiceType == typeof(IActivityDescriptorModifier) &&
            d.ImplementationType == typeof(CronActivityDescriptorModifier));
    }
}
