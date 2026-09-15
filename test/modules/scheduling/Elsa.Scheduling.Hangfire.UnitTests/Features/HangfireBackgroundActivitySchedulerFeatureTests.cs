using Elsa.Common.Multitenancy;
using Elsa.Features.Services;
using Elsa.Scheduling.Hangfire.Features;
using Elsa.Scheduling.Hangfire.Services;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Features;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Elsa.Scheduling.Hangfire.UnitTests.Features;

public class HangfireBackgroundActivitySchedulerFeatureTests
{
    [Fact]
    public void Configure_SetsWorkflowRuntimeBackgroundActivitySchedulerFactory()
    {
        var module = new Mock<IModule>();
        var workflowRuntime = new WorkflowRuntimeFeature(module.Object);
        module.Setup(m => m.Configure(It.IsAny<Action<WorkflowRuntimeFeature>>()))
            .Callback<Action<WorkflowRuntimeFeature>?>(configure => configure?.Invoke(workflowRuntime))
            .Returns(workflowRuntime);

        var feature = new HangfireBackgroundActivitySchedulerFeature(module.Object);
        feature.Configure();

        var expected = new HangfireBackgroundActivityScheduler(
            new BackgroundJobClient(new MemoryStorage()),
            Mock.Of<ITenantAccessor>());
        var services = new ServiceCollection();
        services.AddSingleton(expected);
        using var provider = services.BuildServiceProvider();

        var resolved = workflowRuntime.BackgroundActivityScheduler(provider);

        Assert.Same(expected, resolved);
        Assert.IsNotType<LocalBackgroundActivityScheduler>(resolved);
    }

    [Fact]
    public void Apply_RegistersHangfireBackgroundActivityScheduler()
    {
        var services = new ServiceCollection();
        var module = new Mock<IModule>();
        module.Setup(m => m.Services).Returns(services);

        var feature = new HangfireBackgroundActivitySchedulerFeature(module.Object);
        feature.Apply();

        Assert.Contains(services, d =>
            d.ServiceType == typeof(HangfireBackgroundActivityScheduler) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }
}
