using Elsa.Common.Multitenancy;
using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Scheduling.Quartz.Services;
using Elsa.Scheduling.Quartz.UnitTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Quartz;
using Quartz.Spi;
using QuartzScheduler = Quartz.IScheduler;

namespace Elsa.Scheduling.Quartz.UnitTests.Services;

public class TenantAwareJobFactoryTests
{
    [Fact]
    public void NewJob_TenantIdOnTrigger_ResolvesJobInThatTenantScope()
    {
        // Arrange
        var harness = Harness.Create();
        harness.Tenants.Add("tenant-a");
        var bundle = CreateBundle(triggerTenantId: "tenant-a");

        // Act
        var job = harness.Factory.NewJob(bundle, Scheduler());

        // Assert
        Assert.Equal("tenant-a", harness.Capture.TenantIdAtConstruction);
        Assert.Equal("tenant-a", Assert.Single(harness.Tenants.RequestedIds));
        harness.Factory.ReturnJob(job);
    }

    [Fact]
    public void NewJob_TenantIdOnJob_WhenTriggerHasNone_ResolvesJobInThatTenantScope()
    {
        // Arrange
        var harness = Harness.Create();
        harness.Tenants.Add("tenant-from-job");
        var bundle = CreateBundle(jobTenantId: "tenant-from-job");

        // Act
        var job = harness.Factory.NewJob(bundle, Scheduler());

        // Assert
        Assert.Equal("tenant-from-job", harness.Capture.TenantIdAtConstruction);
        Assert.Equal("tenant-from-job", Assert.Single(harness.Tenants.RequestedIds));
        harness.Factory.ReturnJob(job);
    }

    [Fact]
    public void NewJob_TenantIdOnTrigger_TakesPrecedenceOverJob()
    {
        // Arrange
        var harness = Harness.Create();
        harness.Tenants.Add("tenant-from-trigger");
        harness.Tenants.Add("tenant-from-job");
        var bundle = CreateBundle(triggerTenantId: "tenant-from-trigger", jobTenantId: "tenant-from-job");

        // Act
        var job = harness.Factory.NewJob(bundle, Scheduler());

        // Assert
        Assert.Equal("tenant-from-trigger", harness.Capture.TenantIdAtConstruction);
        Assert.Equal("tenant-from-trigger", Assert.Single(harness.Tenants.RequestedIds));
        harness.Factory.ReturnJob(job);
    }

    [Fact]
    public void NewJob_WhenTenantIsMissing_ThrowsNamingTenantIdJobKeyAndErrorState()
    {
        // Arrange
        var harness = Harness.Create();
        var bundle = CreateBundle(triggerTenantId: "missing-tenant", jobName: "run-workflow");

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => harness.Factory.NewJob(bundle, Scheduler()));

        // Assert
        Assert.Contains("missing-tenant", exception.Message);
        Assert.Contains(bundle.JobDetail.Key.ToString(), exception.Message);
        Assert.Contains("ERROR", exception.Message);
        Assert.Null(harness.Capture.TenantIdAtConstruction);
        Assert.Contains(harness.Logger.Messages, message => message.Contains("ERROR"));
    }

    [Fact]
    public async Task NewJob_WhenTenantLookupThrows_DefersFailureToExecuteRetryPath()
    {
        // Arrange
        var harness = Harness.Create();
        var lookupFailure = new TimeoutException("tenant store down");
        harness.Tenants.ThrowOn("tenant-b", lookupFailure);
        harness.RetryScheduler.Setup(x => x.IsRetryable(lookupFailure)).Returns(true);
        harness.RetryScheduler.Setup(x => x.ScheduleRetryAsync(It.IsAny<IJobExecutionContext>(), lookupFailure, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var (context, _) = QuartzJobTestHelper.CreateJobExecutionContext(new Dictionary<string, object>());
        var bundle = CreateBundle(triggerTenantId: "tenant-b");

        // Act
        var job = harness.Factory.NewJob(bundle, Scheduler());
        await job.Execute(context);

        // Assert
        Assert.Null(harness.Capture.TenantIdAtConstruction);
        harness.RetryScheduler.Verify(x => x.ScheduleRetryAsync(context, lookupFailure, It.IsAny<CancellationToken>()), Times.Once);
        harness.Factory.ReturnJob(job);
    }

    [Fact]
    public void NewJob_WhenNoTenantId_UsesDefaultTenantBehavior()
    {
        // Arrange
        var harness = Harness.Create();
        var bundle = CreateBundle();

        // Act
        var job = harness.Factory.NewJob(bundle, Scheduler());

        // Assert
        Assert.Null(harness.Capture.TenantIdAtConstruction);
        Assert.Equal("default", harness.Capture.ServiceTenantId);
        Assert.Empty(harness.Tenants.RequestedIds);
        harness.Factory.ReturnJob(job);
    }

    [Fact]
    public void NewJob_ForTenantB_ResolvesTenantBScopedServices()
    {
        // Arrange
        var harness = Harness.Create();
        harness.Tenants.Add("tenant-a");
        harness.Tenants.Add("tenant-b");
        var tenantABundle = CreateBundle(triggerTenantId: "tenant-a", jobName: "job-a");
        var tenantBBundle = CreateBundle(triggerTenantId: "tenant-b", jobName: "job-b");

        // Act
        var tenantAJob = harness.Factory.NewJob(tenantABundle, Scheduler());
        var tenantAServiceId = harness.Capture.ServiceTenantId;
        harness.Factory.ReturnJob(tenantAJob);

        var tenantBJob = harness.Factory.NewJob(tenantBBundle, Scheduler());
        var tenantBServiceId = harness.Capture.ServiceTenantId;
        harness.Factory.ReturnJob(tenantBJob);

        // Assert
        Assert.Equal("tenant-a", tenantAServiceId);
        Assert.Equal("tenant-b", tenantBServiceId);
    }

    [Fact]
    public async Task RealScheduler_TenantVisibleDuringConstructionAndExecute()
    {
        // Arrange
        var probe = new SchedulerProbe();
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ITenantAccessor, DefaultTenantAccessor>();
        services.AddSingleton<ITenantFinder, AnyIdTenantFinder>();
        services.AddSingleton(probe);
        services.AddQuartz(quartz => quartz.UseJobFactory<TenantAwareJobFactory>());

        await using var provider = services.BuildServiceProvider();
        var scheduler = await provider.GetRequiredService<ISchedulerFactory>().GetScheduler();
        await scheduler.Start();

        var job = JobBuilder.Create<SchedulerProbeJob>().WithIdentity("probe").Build();
        var trigger = TriggerBuilder.Create()
            .ForJob(job)
            .UsingJobData("TenantId", "tenant-b")
            .StartNow()
            .Build();

        // Act
        await scheduler.ScheduleJob(job, trigger);
        await probe.Done.Task.WaitAsync(TimeSpan.FromSeconds(20));
        await scheduler.Shutdown(waitForJobsToComplete: true);

        // Assert
        Assert.Equal("tenant-b", probe.AtConstruction);
        Assert.Equal("tenant-b", probe.AtExecute);
    }

    private static QuartzScheduler Scheduler() => Mock.Of<QuartzScheduler>(s => s.Context == new SchedulerContext());

    private static TriggerFiredBundle CreateBundle(string? triggerTenantId = null, string? jobTenantId = null, string jobName = "test-job")
    {
        var jobBuilder = JobBuilder.Create<ProbeJob>().WithIdentity(jobName);
        if (jobTenantId is not null)
            jobBuilder.UsingJobData("TenantId", jobTenantId);
        var job = jobBuilder.Build();

        var triggerBuilder = TriggerBuilder.Create().ForJob(job).WithIdentity($"{jobName}-trigger");
        if (triggerTenantId is not null)
            triggerBuilder.UsingJobData("TenantId", triggerTenantId);
        var trigger = (IOperableTrigger)triggerBuilder.Build();

        return new TriggerFiredBundle(job, trigger, null, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null);
    }

    private sealed class Harness
    {
        private Harness(
            TenantAwareJobFactory factory,
            FakeTenantFinder tenants,
            ProbeCapture capture,
            CapturingLogger logger,
            Mock<IQuartzJobRetryScheduler> retryScheduler)
        {
            Factory = factory;
            Tenants = tenants;
            Capture = capture;
            Logger = logger;
            RetryScheduler = retryScheduler;
        }

        public TenantAwareJobFactory Factory { get; }
        public FakeTenantFinder Tenants { get; }
        public ProbeCapture Capture { get; }
        public CapturingLogger Logger { get; }
        public Mock<IQuartzJobRetryScheduler> RetryScheduler { get; }

        public static Harness Create()
        {
            var tenants = new FakeTenantFinder();
            var capture = new ProbeCapture();
            var logger = new CapturingLogger();
            var retryScheduler = new Mock<IQuartzJobRetryScheduler>();
            var options = Microsoft.Extensions.Options.Options.Create(new QuartzOptions());
            var services = new ServiceCollection();
            services.AddSingleton<ITenantAccessor, DefaultTenantAccessor>();
            services.AddSingleton<ITenantFinder>(tenants);
            services.AddSingleton(capture);
            services.AddSingleton<ILogger<TenantAwareJobFactory>>(logger);
            services.AddSingleton(options);
            services.AddSingleton(retryScheduler.Object);
            services.AddTransient<ProbeJob>();
            services.AddScoped<ITenantBoundService>(sp =>
            {
                var tenantId = sp.GetRequiredService<ITenantAccessor>().Tenant?.Id ?? "default";
                return new TenantBoundService(tenantId);
            });

            var provider = services.BuildServiceProvider();
            var factory = new TenantAwareJobFactory(provider, options, logger);

            return new Harness(factory, tenants, capture, logger, retryScheduler);
        }
    }

    private sealed class FakeTenantFinder : ITenantFinder
    {
        private readonly Dictionary<string, Tenant> _tenants = new();
        private readonly Dictionary<string, Exception> _failures = new();

        public List<string> RequestedIds { get; } = [];

        public void Add(string tenantId) => _tenants[tenantId] = new Tenant { Id = tenantId, Name = tenantId };

        public void ThrowOn(string tenantId, Exception exception) => _failures[tenantId] = exception;

        public Task<Tenant?> FindByIdAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            RequestedIds.Add(tenantId);
            if (_failures.TryGetValue(tenantId, out var exception))
                throw exception;

            return Task.FromResult(_tenants.TryGetValue(tenantId, out var tenant) ? tenant : null);
        }
    }

    private sealed class AnyIdTenantFinder : ITenantFinder
    {
        public Task<Tenant?> FindByIdAsync(string tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Tenant?>(new Tenant { Id = tenantId, Name = tenantId });
    }

    private sealed class ProbeCapture
    {
        public string? TenantIdAtConstruction { get; set; }
        public string? ServiceTenantId { get; set; }
    }

    public sealed class SchedulerProbe
    {
        public string? AtConstruction { get; set; }
        public string? AtExecute { get; set; }
        public TaskCompletionSource Done { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class SchedulerProbeJob : IJob
    {
        private readonly ITenantAccessor _accessor;
        private readonly SchedulerProbe _probe;

        public SchedulerProbeJob(ITenantAccessor accessor, SchedulerProbe probe)
        {
            _accessor = accessor;
            _probe = probe;
            probe.AtConstruction = accessor.Tenant?.Id;
        }

        public Task Execute(IJobExecutionContext context)
        {
            _probe.AtExecute = _accessor.Tenant?.Id;
            _probe.Done.TrySetResult();
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingLogger : ILogger<TenantAwareJobFactory>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }

    private interface ITenantBoundService
    {
        string TenantId { get; }
    }

    private sealed class TenantBoundService(string tenantId) : ITenantBoundService
    {
        public string TenantId { get; } = tenantId;
    }

    private sealed class ProbeJob : IJob
    {
        public ProbeJob(ITenantAccessor tenantAccessor, ITenantBoundService tenantBoundService, ProbeCapture capture)
        {
            capture.TenantIdAtConstruction = tenantAccessor.Tenant?.Id;
            capture.ServiceTenantId = tenantBoundService.TenantId;
        }

        public Task Execute(IJobExecutionContext context) => Task.CompletedTask;
    }
}
