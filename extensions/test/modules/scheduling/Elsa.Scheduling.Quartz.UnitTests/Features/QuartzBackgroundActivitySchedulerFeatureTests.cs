using Elsa.Common.Multitenancy;
using Elsa.Features.Services;
using Elsa.Scheduling.Quartz.Features;
using Elsa.Scheduling.Quartz.Services;
using Elsa.Scheduling.Quartz.ShellFeatures;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Quartz;

namespace Elsa.Scheduling.Quartz.UnitTests.Features;

public class QuartzBackgroundActivitySchedulerFeatureTests
{
    [Fact]
    public void Configure_SetsWorkflowRuntimeBackgroundActivitySchedulerFactory()
    {
        var module = new Mock<IModule>();
        var workflowRuntime = new WorkflowRuntimeFeature(module.Object);
        module.Setup(m => m.Configure(It.IsAny<Action<WorkflowRuntimeFeature>>()))
            .Callback<Action<WorkflowRuntimeFeature>?>(configure => configure?.Invoke(workflowRuntime))
            .Returns(workflowRuntime);

        var feature = new QuartzBackgroundActivitySchedulerFeature(module.Object);
        feature.Configure();

        var expected = new QuartzBackgroundActivityScheduler(
            Mock.Of<ISchedulerFactory>(),
            Mock.Of<ITenantAccessor>(),
            NullLogger<QuartzBackgroundActivityScheduler>.Instance);
        var services = new ServiceCollection();
        services.AddSingleton(expected);
        var provider = services.BuildServiceProvider();

        var resolved = workflowRuntime.BackgroundActivityScheduler(provider);

        Assert.Same(expected, resolved);
        Assert.IsNotType<LocalBackgroundActivityScheduler>(resolved);
    }

    [Fact]
    public void Apply_RegistersQuartzBackgroundActivityScheduler()
    {
        var services = new ServiceCollection();
        var module = new Mock<IModule>();
        module.Setup(m => m.Services).Returns(services);

        var feature = new QuartzBackgroundActivitySchedulerFeature(module.Object);
        feature.Apply();

        Assert.Contains(services, d =>
            d.ServiceType == typeof(QuartzBackgroundActivityScheduler) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void ShellFeature_ReplacesIBackgroundActivitySchedulerRegistration()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBackgroundActivityScheduler, FakeLocalBackgroundActivityScheduler>();

        new QuartzBackgroundActivitySchedulerShellFeature().ConfigureServices(services);

        Assert.DoesNotContain(services, d =>
            d.ServiceType == typeof(IBackgroundActivityScheduler) &&
            d.ImplementationType == typeof(FakeLocalBackgroundActivityScheduler));
        Assert.Contains(services, d => d.ServiceType == typeof(QuartzBackgroundActivityScheduler));
        var schedulerRegistration = Assert.Single(services, d => d.ServiceType == typeof(IBackgroundActivityScheduler));
        Assert.NotNull(schedulerRegistration.ImplementationFactory);
        Assert.Equal(ServiceLifetime.Singleton, schedulerRegistration.Lifetime);
    }

    private sealed class FakeLocalBackgroundActivityScheduler : IBackgroundActivityScheduler
    {
        public Task<string> CreateAsync(ScheduledBackgroundActivity scheduledBackgroundActivity, CancellationToken cancellationToken = default) => Task.FromResult("local");
        public Task ScheduleAsync(string jobId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<string> ScheduleAsync(ScheduledBackgroundActivity scheduledBackgroundActivity, CancellationToken cancellationToken = default) => Task.FromResult("local");
        public Task UnscheduledAsync(string jobId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CancelAsync(string jobId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
