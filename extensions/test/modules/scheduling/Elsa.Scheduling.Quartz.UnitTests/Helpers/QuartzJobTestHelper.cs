using Elsa.Common.Multitenancy;
using Elsa.Resilience;
using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Scheduling.Quartz.Options;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Quartz;
using QuartzScheduler = Quartz.IScheduler;

namespace Elsa.Scheduling.Quartz.UnitTests.Helpers;

/// <summary>
/// Provides common helper methods for testing Quartz jobs.
/// </summary>
public static class QuartzJobTestHelper
{
    /// <summary>
    /// Creates a mock job execution context with the specified job data.
    /// </summary>
    public static (IJobExecutionContext Context, Mock<QuartzScheduler> Scheduler) CreateJobExecutionContext(
        IDictionary<string, object> jobData,
        string? jobKeyName = null,
        IDictionary<string, object>? triggerData = null,
        string? triggerName = null)
    {
        var jobDataMap = new JobDataMap(jobData);
        var triggerDataMap = new JobDataMap(triggerData ?? new Dictionary<string, object>());
        var mergedDataMap = new JobDataMap(jobData);
        mergedDataMap.PutAll(triggerDataMap);
        var jobKey = new JobKey(jobKeyName ?? "test-job");
        var triggerKey = new TriggerKey(triggerName ?? "test-trigger");

        var jobDetail = new Mock<IJobDetail>();
        jobDetail.Setup(j => j.Key).Returns(jobKey);
        jobDetail.Setup(j => j.JobDataMap).Returns(jobDataMap);

        var trigger = new Mock<ITrigger>();
        trigger.Setup(t => t.Key).Returns(triggerKey);
        trigger.Setup(t => t.JobKey).Returns(jobKey);
        trigger.Setup(t => t.JobDataMap).Returns(triggerDataMap);

        var scheduler = new Mock<QuartzScheduler>();
        scheduler.Setup(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DateTimeOffset.Now);
        scheduler.Setup(s => s.UnscheduleJob(It.IsAny<TriggerKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        scheduler.Setup(s => s.RescheduleJob(It.IsAny<TriggerKey>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DateTimeOffset.Now);
        scheduler.Setup(s => s.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var context = new Mock<IJobExecutionContext>();
        context.Setup(c => c.MergedJobDataMap).Returns(mergedDataMap);
        context.Setup(c => c.JobDetail).Returns(jobDetail.Object);
        context.Setup(c => c.Trigger).Returns(trigger.Object);
        context.Setup(c => c.Scheduler).Returns(scheduler.Object);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        return (context.Object, scheduler);
    }

    /// <summary>
    /// Creates <see cref="QuartzJobOptions"/> with deterministic retry settings: jitter is disabled so that computed
    /// delays are exact.
    /// </summary>
    public static QuartzJobOptions CreateQuartzJobOptions(Action<QuartzJobOptions>? configure = null)
    {
        var options = new QuartzJobOptions
        {
            InitialRetryDelay = TimeSpan.FromSeconds(1),
            UseJitter = false
        };

        configure?.Invoke(options);
        return options;
    }

    /// <summary>
    /// Wraps the specified options so they can be injected into a service that takes <see cref="IOptions{TOptions}"/>.
    /// </summary>
    public static IOptions<QuartzJobOptions> AsOptions(this QuartzJobOptions options) => Microsoft.Extensions.Options.Options.Create(options);

    /// <summary>
    /// Creates a mock tenant accessor that allows context pushing.
    /// </summary>
    public static Mock<ITenantAccessor> CreateTenantAccessor()
    {
        var tenantAccessor = new Mock<ITenantAccessor>();
        tenantAccessor.Setup(t => t.PushContext(It.IsAny<Tenant>())).Returns((IDisposable)null!);
        return tenantAccessor;
    }

    extension(Mock<IWorkflowStarter> workflowStarter)
    {
        /// <summary>
        /// Sets up a workflow starter to return the specified response.
        /// </summary>
        public void SetupStartWorkflow(StartWorkflowResponse response) =>
            workflowStarter.Setup(w => w.StartWorkflowAsync(It.IsAny<StartWorkflowRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

        /// <summary>
        /// Sets up a workflow starter to throw the specified exception.
        /// </summary>
        public void SetupStartWorkflowThrows(Exception exception) =>
            workflowStarter.Setup(w => w.StartWorkflowAsync(It.IsAny<StartWorkflowRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);
    }

    /// <summary>
    /// Sets up a transient exception detector to return the specified value for any exception.
    /// </summary>
    public static void SetupIsTransient(this Mock<ITransientExceptionDetector> detector, bool isTransient) =>
        detector.Setup(d => d.IsTransient(It.IsAny<Exception>())).Returns(isTransient);

    extension(Mock<IWorkflowRuntime> workflowRuntime)
    {
        /// <summary>
        /// Sets up a workflow runtime to return the specified workflow client.
        /// </summary>
        public void SetupCreateClient(Mock<IWorkflowClient> workflowClient) =>
            workflowRuntime.Setup(r => r.CreateClientAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(workflowClient.Object);

        /// <summary>
        /// Sets up a workflow runtime to throw the specified exception.
        /// </summary>
        public void SetupCreateClientThrows(Exception exception) =>
            workflowRuntime.Setup(r => r.CreateClientAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);
    }

    /// <summary>
    /// Sets up a workflow client to return the specified response.
    /// </summary>
    public static void SetupRunInstance(this Mock<IWorkflowClient> workflowClient, RunWorkflowInstanceResponse? response = null) =>
        workflowClient.Setup(c => c.RunInstanceAsync(It.IsAny<RunWorkflowInstanceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response ?? new RunWorkflowInstanceResponse());

    extension(Mock<QuartzScheduler> scheduler)
    {
        /// <summary>
        /// Verifies that the scheduler scheduled a trigger exactly once.
        /// </summary>
        public void VerifyScheduled() =>
            scheduler.Verify(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Once);

        /// <summary>
        /// Verifies that the scheduler rescheduled a job exactly once.
        /// </summary>
        public void VerifyRescheduled() =>
            scheduler.Verify(s => s.RescheduleJob(It.IsAny<TriggerKey>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Once);

        /// <summary>
        /// Verifies that the scheduler deleted a job exactly once.
        /// </summary>
        public void VerifyDeleted() =>
            scheduler.Verify(s => s.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Once);

        /// <summary>
        /// Verifies that the scheduler did not delete a job.
        /// </summary>
        public void VerifyNotDeleted() =>
            scheduler.Verify(s => s.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Never);

        /// <summary>
        /// Verifies that the scheduler unscheduled a job exactly once.
        /// </summary>
        public void VerifyUnscheduled() =>
            scheduler.Verify(s => s.UnscheduleJob(It.IsAny<TriggerKey>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that the workflow starter was called exactly once.
    /// </summary>
    public static void VerifyStartWorkflowCalled(this Mock<IWorkflowStarter> workflowStarter) =>
        workflowStarter.Verify(w => w.StartWorkflowAsync(It.IsAny<StartWorkflowRequest>(), It.IsAny<CancellationToken>()), Times.Once);

    /// <summary>
    /// Verifies that the workflow client was called exactly once.
    /// </summary>
    public static void VerifyRunInstanceCalled(this Mock<IWorkflowClient> workflowClient) =>
        workflowClient.Verify(c => c.RunInstanceAsync(It.IsAny<RunWorkflowInstanceRequest>(), It.IsAny<CancellationToken>()), Times.Once);

    extension<T>(Mock<ILogger<T>> logger)
    {
        /// <summary>
        /// Verifies that the logger logged at the specified level the specified number of times.
        /// </summary>
        public void VerifyLogged(LogLevel level, Times times) =>
            logger.Verify(
                x => x.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                times);
    }

    extension(Mock<IQuartzJobRetryScheduler> retryScheduler)
    {
        /// <summary>
        /// Sets up the retry scheduler to consider any exception retryable, and to report whether it scheduled a retry.
        /// </summary>
        public void SetupRetry(bool isRetryable, bool retryScheduled = true)
        {
            retryScheduler.Setup(x => x.IsRetryable(It.IsAny<Exception>())).Returns(isRetryable);
            retryScheduler.Setup(x => x.ScheduleRetryAsync(It.IsAny<IJobExecutionContext>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(retryScheduled);
        }

        /// <summary>
        /// Verifies that a retry was requested for the specified context exactly once.
        /// </summary>
        public void VerifyRetryScheduled(IJobExecutionContext context) =>
            retryScheduler.Verify(x => x.ScheduleRetryAsync(context, It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
