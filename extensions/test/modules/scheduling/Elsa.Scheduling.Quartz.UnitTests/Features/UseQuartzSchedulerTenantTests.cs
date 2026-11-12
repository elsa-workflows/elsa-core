using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Scheduling.Quartz.Jobs;
using Elsa.Scheduling.Quartz.Services;
using Elsa.Scheduling.Quartz.UnitTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Quartz;
using Quartz.Spi;

namespace Elsa.Scheduling.Quartz.UnitTests.Features;

public class UseQuartzSchedulerTenantTests
{
    [Fact]
    public async Task UseQuartzScheduler_WithoutUseQuartz_TwoTenants_SeeOwnTenantAtConstructionAndExecute()
    {
        var probe = new TwoTenantProbe();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(probe);

        var module = services.CreateModule();
        module.UseScheduling(scheduling => scheduling.UseQuartzScheduler());
        module.Apply();

        services.RemoveAll<ITenantFinder>();
        services.AddSingleton<ITenantFinder, AnyIdTenantFinder>();

        await using var provider = services.BuildServiceProvider();
        Assert.IsType<TenantAwareJobFactory>(provider.GetRequiredService<IJobFactory>());

        var scheduler = await provider.GetRequiredService<ISchedulerFactory>().GetScheduler();
        await scheduler.Start();

        try
        {
            await FireAndAssert(scheduler, probe, "tenant-a");
            probe.Reset();
            await FireAndAssert(scheduler, probe, "tenant-b");
        }
        finally
        {
            await scheduler.Shutdown(waitForJobsToComplete: true);
        }
    }

    [Fact]
    public async Task HostCustomJobFactory_IsKept_AndGuardThrowsWhenTenantMissingFromContext()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IJobFactory, HostJobFactory>();
        var module = services.CreateModule();
        module.UseScheduling(scheduling => scheduling.UseQuartzScheduler());
        module.Apply();

        var factory = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IJobFactory) && descriptor.ImplementationType == typeof(HostJobFactory));
        Assert.Equal(typeof(HostJobFactory), factory.ImplementationType);
        Assert.DoesNotContain(services, descriptor =>
            descriptor.ServiceType == typeof(IJobFactory) &&
            descriptor.ImplementationType == typeof(TenantAwareJobFactory));

        var starter = new Mock<Elsa.Workflows.Runtime.IWorkflowStarter>();
        var logger = new Mock<ILogger<RunWorkflowJob>>();
        var job = new RunWorkflowJob(
            new DefaultTenantAccessor(),
            new AnyIdTenantFinder(),
            starter.Object,
            Mock.Of<Elsa.Scheduling.Quartz.Contracts.IQuartzJobRetryScheduler>(),
            logger.Object);
        var (context, _) = QuartzJobTestHelper.CreateJobExecutionContext(
            new Dictionary<string, object> { ["DefinitionVersionId"] = "def-1" },
            triggerData: new Dictionary<string, object> { ["TenantId"] = "tenant-b" });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => job.Execute(context));

        Assert.Contains("tenant-b", exception.Message);
        Assert.Contains(context.JobDetail.Key.ToString(), exception.Message);
        starter.Verify(x => x.StartWorkflowAsync(It.IsAny<Elsa.Workflows.Runtime.StartWorkflowRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static async Task FireAndAssert(global::Quartz.IScheduler scheduler, TwoTenantProbe probe, string tenantId)
    {
        var job = JobBuilder.Create<TwoTenantProbeJob>().WithIdentity($"probe-{tenantId}").Build();
        var trigger = TriggerBuilder.Create()
            .ForJob(job)
            .UsingJobData("TenantId", tenantId)
            .StartNow()
            .Build();

        await scheduler.ScheduleJob(job, trigger);
        await probe.Done.Task.WaitAsync(TimeSpan.FromSeconds(20));

        Assert.Equal(tenantId, probe.AtConstruction);
        Assert.Equal(tenantId, probe.AtExecute);
    }

    public sealed class TwoTenantProbe
    {
        public string? AtConstruction { get; set; }
        public string? AtExecute { get; set; }
        public TaskCompletionSource Done { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Reset()
        {
            AtConstruction = null;
            AtExecute = null;
            Done = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    public sealed class TwoTenantProbeJob : IJob
    {
        public TwoTenantProbeJob(ITenantAccessor accessor, TwoTenantProbe probe)
        {
            probe.AtConstruction = accessor.Tenant?.Id;
            _accessor = accessor;
            _probe = probe;
        }

        private readonly ITenantAccessor _accessor;
        private readonly TwoTenantProbe _probe;

        public Task Execute(IJobExecutionContext context)
        {
            _probe.AtExecute = _accessor.Tenant?.Id;
            _probe.Done.TrySetResult();
            return Task.CompletedTask;
        }
    }

    private sealed class HostJobFactory : IJobFactory
    {
        public IJob NewJob(TriggerFiredBundle bundle, global::Quartz.IScheduler scheduler) => throw new NotSupportedException();
        public void ReturnJob(IJob job)
        {
        }
    }
}
