using Elsa.Common.Multitenancy;
using Elsa.Scheduling.Quartz.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        var job = harness.Factory.NewJob(bundle, Mock.Of<QuartzScheduler>());

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
        var job = harness.Factory.NewJob(bundle, Mock.Of<QuartzScheduler>());

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
        var job = harness.Factory.NewJob(bundle, Mock.Of<QuartzScheduler>());

        // Assert
        Assert.Equal("tenant-from-trigger", harness.Capture.TenantIdAtConstruction);
        Assert.Equal("tenant-from-trigger", Assert.Single(harness.Tenants.RequestedIds));
        harness.Factory.ReturnJob(job);
    }

    [Fact]
    public void NewJob_WhenTenantIsMissing_ThrowsNamingTenantIdAndJobKey()
    {
        // Arrange
        var harness = Harness.Create();
        var bundle = CreateBundle(triggerTenantId: "missing-tenant", jobName: "run-workflow");

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => harness.Factory.NewJob(bundle, Mock.Of<QuartzScheduler>()));

        // Assert
        Assert.Contains("missing-tenant", exception.Message);
        Assert.Contains(bundle.JobDetail.Key.ToString(), exception.Message);
        Assert.Null(harness.Capture.TenantIdAtConstruction);
        Assert.Contains(LogLevel.Error, harness.Logger.Levels);
    }

    [Fact]
    public void NewJob_WhenNoTenantId_UsesDefaultTenantBehavior()
    {
        // Arrange
        var harness = Harness.Create();
        var bundle = CreateBundle();

        // Act
        var job = harness.Factory.NewJob(bundle, Mock.Of<QuartzScheduler>());

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
        var tenantAJob = harness.Factory.NewJob(tenantABundle, Mock.Of<QuartzScheduler>());
        var tenantAServiceId = harness.Capture.ServiceTenantId;
        harness.Factory.ReturnJob(tenantAJob);

        var tenantBJob = harness.Factory.NewJob(tenantBBundle, Mock.Of<QuartzScheduler>());
        var tenantBServiceId = harness.Capture.ServiceTenantId;
        harness.Factory.ReturnJob(tenantBJob);

        // Assert
        Assert.Equal("tenant-a", tenantAServiceId);
        Assert.Equal("tenant-b", tenantBServiceId);
    }

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
        private Harness(TenantAwareJobFactory factory, FakeTenantFinder tenants, ProbeCapture capture, CapturingLogger logger)
        {
            Factory = factory;
            Tenants = tenants;
            Capture = capture;
            Logger = logger;
        }

        public TenantAwareJobFactory Factory { get; }
        public FakeTenantFinder Tenants { get; }
        public ProbeCapture Capture { get; }
        public CapturingLogger Logger { get; }

        public static Harness Create()
        {
            var tenants = new FakeTenantFinder();
            var capture = new ProbeCapture();
            var logger = new CapturingLogger();
            var services = new ServiceCollection();
            services.AddSingleton<ITenantAccessor, DefaultTenantAccessor>();
            services.AddSingleton<ITenantFinder>(tenants);
            services.AddSingleton(capture);
            services.AddSingleton<ILogger<TenantAwareJobFactory>>(logger);
            services.AddTransient<ProbeJob>();
            services.AddScoped<ITenantBoundService>(sp =>
            {
                var tenantId = sp.GetRequiredService<ITenantAccessor>().Tenant?.Id ?? "default";
                return new TenantBoundService(tenantId);
            });

            var provider = services.BuildServiceProvider();
            var factory = new TenantAwareJobFactory(provider, logger);

            return new Harness(factory, tenants, capture, logger);
        }
    }

    private sealed class FakeTenantFinder : ITenantFinder
    {
        private readonly Dictionary<string, Tenant> _tenants = new();

        public List<string> RequestedIds { get; } = [];

        public void Add(string tenantId) => _tenants[tenantId] = new Tenant { Id = tenantId, Name = tenantId };

        public Task<Tenant?> FindByIdAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            RequestedIds.Add(tenantId);
            return Task.FromResult(_tenants.TryGetValue(tenantId, out var tenant) ? tenant : null);
        }
    }

    private sealed class ProbeCapture
    {
        public string? TenantIdAtConstruction { get; set; }
        public string? ServiceTenantId { get; set; }
    }

    private sealed class CapturingLogger : ILogger<TenantAwareJobFactory>
    {
        public List<LogLevel> Levels { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Levels.Add(logLevel);

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
