using Elsa.Common.Multitenancy;
using Elsa.Scheduling.Hangfire.Handlers;
using Elsa.Scheduling.Hangfire.Services;
using Elsa.Scheduling.Hangfire.ShellFeatures;
using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Elsa.Scheduling.Hangfire.UnitTests.ShellFeatures;

public class HangfireShellFeatureTests
{
    [Fact]
    public void HangfireShellFeature_RegistersMemoryStorage()
    {
        var services = new ServiceCollection();

        new HangfireShellFeature().ConfigureServices(services);

        using var provider = services.BuildServiceProvider();
        Assert.IsType<MemoryStorage>(provider.GetRequiredService<JobStorage>());
    }

    [Fact]
    public void HangfireShellFeature_DoesNotExposeUseMemoryStorageSetting()
    {
        Assert.Null(typeof(HangfireShellFeature).GetProperty("UseMemoryStorage"));
    }

    [Fact]
    public void SchedulerShellFeature_ReplacesIWorkflowSchedulerRegistration()
    {
        var services = new ServiceCollection();
        services.AddScoped<IWorkflowScheduler, FakeWorkflowScheduler>();

        new HangfireSchedulerShellFeature().ConfigureServices(services);

        Assert.DoesNotContain(services, d =>
            d.ServiceType == typeof(IWorkflowScheduler) &&
            d.ImplementationType == typeof(FakeWorkflowScheduler));
        Assert.Contains(services, d => d.ServiceType == typeof(HangfireWorkflowScheduler));
        Assert.Contains(services, d =>
            d.ServiceType == typeof(IActivityDescriptorModifier) &&
            d.ImplementationType == typeof(CronActivityDescriptorModifier));

        var schedulerRegistration = Assert.Single(services, d => d.ServiceType == typeof(IWorkflowScheduler));
        Assert.NotNull(schedulerRegistration.ImplementationFactory);
        Assert.Equal(ServiceLifetime.Singleton, schedulerRegistration.Lifetime);
    }

    [Fact]
    public void BackgroundActivitySchedulerShellFeature_ReplacesIBackgroundActivitySchedulerRegistration()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBackgroundActivityScheduler, FakeLocalBackgroundActivityScheduler>();

        new HangfireBackgroundActivitySchedulerShellFeature().ConfigureServices(services);

        Assert.DoesNotContain(services, d =>
            d.ServiceType == typeof(IBackgroundActivityScheduler) &&
            d.ImplementationType == typeof(FakeLocalBackgroundActivityScheduler));
        Assert.Contains(services, d => d.ServiceType == typeof(HangfireBackgroundActivityScheduler));

        var schedulerRegistration = Assert.Single(services, d => d.ServiceType == typeof(IBackgroundActivityScheduler));
        Assert.NotNull(schedulerRegistration.ImplementationFactory);
        Assert.Equal(ServiceLifetime.Singleton, schedulerRegistration.Lifetime);
    }

    [Fact]
    public void EnablingSchedulerShellFeatures_ResolvesHangfireContracts()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<ITenantAccessor>());
        services.AddScoped<IWorkflowScheduler, FakeWorkflowScheduler>();
        services.AddSingleton<IBackgroundActivityScheduler, FakeLocalBackgroundActivityScheduler>();

        new HangfireShellFeature().ConfigureServices(services);
        new HangfireSchedulerShellFeature().ConfigureServices(services);
        new HangfireBackgroundActivitySchedulerShellFeature().ConfigureServices(services);

        using var provider = services.BuildServiceProvider();

        Assert.IsType<HangfireWorkflowScheduler>(provider.GetRequiredService<IWorkflowScheduler>());
        Assert.IsType<HangfireBackgroundActivityScheduler>(provider.GetRequiredService<IBackgroundActivityScheduler>());
    }

    private sealed class FakeWorkflowScheduler : IWorkflowScheduler
    {
        public ValueTask ScheduleAtAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, DateTimeOffset at, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask ScheduleAtAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, DateTimeOffset at, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask ScheduleRecurringAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask ScheduleRecurringAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask ScheduleCronAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, string cronExpression, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask ScheduleCronAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, string cronExpression, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask UnscheduleAsync(string taskName, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
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
