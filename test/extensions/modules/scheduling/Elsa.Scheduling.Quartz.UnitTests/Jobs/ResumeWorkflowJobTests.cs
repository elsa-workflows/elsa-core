using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Scheduling.Quartz.Jobs;
using Elsa.Scheduling.Quartz.UnitTests.Helpers;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Exceptions;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using QuartzScheduler = Quartz.IScheduler;

namespace Elsa.Scheduling.Quartz.UnitTests.Jobs;

public class ResumeWorkflowJobTests
{
    private readonly Mock<IWorkflowRuntime> _workflowRuntime = new();
    private readonly Mock<IJsonSerializer> _jsonSerializer = new();
    private readonly Mock<ITenantFinder> _tenantFinder = new();
    private readonly Mock<ITenantAccessor> _tenantAccessor = QuartzJobTestHelper.CreateTenantAccessor();
    private readonly Mock<IQuartzJobRetryScheduler> _retryScheduler = new();
    private readonly Mock<ILogger<ResumeWorkflowJob>> _logger = new();
    private readonly ResumeWorkflowJob _job;

    public ResumeWorkflowJobTests()
    {
        _job = new(
            _workflowRuntime.Object,
            _jsonSerializer.Object,
            _tenantFinder.Object,
            _tenantAccessor.Object,
            _retryScheduler.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Execute_SuccessfulResume_CallsWorkflowClient()
    {
        var (context, _) = CreateJobExecutionContext();
        var workflowClient = new Mock<IWorkflowClient>();
        workflowClient.SetupRunInstance();
        _workflowRuntime.SetupCreateClient(workflowClient);

        await _job.Execute(context);

        workflowClient.VerifyRunInstanceCalled();
    }

    [Fact]
    public async Task Execute_WorkflowGraphNotFound_UnschedulesTrigger()
    {
        var (context, scheduler) = CreateJobExecutionContext();
        var handle = WorkflowDefinitionHandle.ByDefinitionVersionId("workflow-def-123");
        _workflowRuntime.SetupCreateClientThrows(new WorkflowGraphNotFoundException("Not found", handle));

        await _job.Execute(context);

        scheduler.Verify(s => s.UnscheduleJob(new TriggerKey("test-trigger"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_WorkflowGraphNotFoundOnStaleOriginal_DoesNotUnscheduleReplacementOriginal()
    {
        var jobData = new Dictionary<string, object>
        {
            ["WorkflowInstanceId"] = "workflow-instance-123",
            ["BookmarkId"] = "bookmark-123"
        };
        var (context, scheduler) = QuartzJobTestHelper.CreateJobExecutionContext(
            jobData,
            triggerName: "task-1",
            triggerData: new Dictionary<string, object>
            {
                [QuartzJobDataKeys.RetryScheduleGeneration] = "old-generation"
            });
        var handle = WorkflowDefinitionHandle.ByDefinitionVersionId("workflow-def-123");
        _workflowRuntime.SetupCreateClientThrows(new WorkflowGraphNotFoundException("Not found", handle));
        var replacementTrigger = TriggerBuilder.Create()
            .WithIdentity(context.Trigger.Key)
            .UsingJobData(QuartzJobDataKeys.RetryScheduleGeneration, "new-generation")
            .Build();
        scheduler.Setup(s => s.GetTrigger(context.Trigger.Key, It.IsAny<CancellationToken>())).ReturnsAsync(replacementTrigger);
        var replacementRetryTrigger = TriggerBuilder.Create()
            .WithIdentity(QuartzTriggerKeys.GetRetryTriggerKey(context.Trigger.Key))
            .UsingJobData(QuartzJobDataKeys.RetryScheduleGeneration, "new-generation")
            .Build();
        scheduler.Setup(s => s.GetTrigger(replacementRetryTrigger.Key, It.IsAny<CancellationToken>())).ReturnsAsync(replacementRetryTrigger);

        await _job.Execute(context);

        scheduler.Verify(s => s.UnscheduleJob(context.Trigger.Key, It.IsAny<CancellationToken>()), Times.Never);
        scheduler.Verify(s => s.UnscheduleJob(QuartzTriggerKeys.GetRetryTriggerKey(context.Trigger.Key), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_WorkflowGraphNotFoundOnStaleRetry_DoesNotUnscheduleReplacementOriginal()
    {
        var jobData = new Dictionary<string, object>
        {
            ["WorkflowInstanceId"] = "workflow-instance-123",
            ["BookmarkId"] = "bookmark-123"
        };
        var (context, scheduler) = QuartzJobTestHelper.CreateJobExecutionContext(
            jobData,
            triggerName: "task-retry-retry",
            triggerData: new Dictionary<string, object>
            {
                [QuartzJobDataKeys.RetryTrigger] = bool.TrueString,
                [QuartzJobDataKeys.RetryOriginalTriggerName] = "task-retry",
                [QuartzJobDataKeys.RetryOriginalTriggerGroup] = new TriggerKey("task-retry").Group,
                [QuartzJobDataKeys.RetryScheduleGeneration] = "old-generation"
            });
        var handle = WorkflowDefinitionHandle.ByDefinitionVersionId("workflow-def-123");
        _workflowRuntime.SetupCreateClientThrows(new WorkflowGraphNotFoundException("Not found", handle));
        var replacementTrigger = TriggerBuilder.Create()
            .WithIdentity("task-retry")
            .ForJob(new JobKey("test-job"))
            .UsingJobData(QuartzJobDataKeys.RetryScheduleGeneration, "new-generation")
            .Build();
        scheduler.Setup(s => s.GetTrigger(new TriggerKey("task-retry"), It.IsAny<CancellationToken>())).ReturnsAsync(replacementTrigger);
        var replacementRetryTrigger = TriggerBuilder.Create()
            .WithIdentity(QuartzTriggerKeys.GetRetryTriggerKey(replacementTrigger.Key))
            .ForJob(new JobKey("test-job"))
            .UsingJobData(QuartzJobDataKeys.RetryScheduleGeneration, "new-generation")
            .Build();
        scheduler.Setup(s => s.GetTrigger(replacementRetryTrigger.Key, It.IsAny<CancellationToken>())).ReturnsAsync(replacementRetryTrigger);

        await _job.Execute(context);

        scheduler.Verify(s => s.UnscheduleJob(QuartzTriggerKeys.GetRetryTriggerKey(replacementTrigger.Key), It.IsAny<CancellationToken>()), Times.Never);
        scheduler.Verify(s => s.UnscheduleJob(new TriggerKey("task-retry"), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(typeof(HttpRequestException))]
    [InlineData(typeof(TimeoutException))]
    public async Task Execute_RetryableException_ReschedulesJob(Type exceptionType)
    {
        var (context, scheduler) = CreateJobExecutionContext();
        _retryScheduler.SetupRetry(isRetryable: true);
        _workflowRuntime.SetupCreateClientThrows((Exception)Activator.CreateInstance(exceptionType)!);

        await _job.Execute(context);

        _retryScheduler.VerifyRetryScheduled(context);
        _logger.VerifyLogged(LogLevel.Error, Times.Never());
        scheduler.VerifyNotDeleted();
    }

    [Fact]
    public async Task Execute_RetriesExhausted_LogsErrorAndKeepsTheJob()
    {
        var (context, scheduler) = CreateJobExecutionContext();
        _retryScheduler.SetupRetry(isRetryable: true, retryScheduled: false);
        _workflowRuntime.SetupCreateClientThrows(new TimeoutException());

        await _job.Execute(context);

        _retryScheduler.VerifyRetryScheduled(context);
        _logger.VerifyLogged(LogLevel.Error, Times.Once());
        scheduler.VerifyNotDeleted();
    }

    [Fact]
    public async Task Execute_IsRetryableOverrideRejectsTheException_TreatsItAsPermanent()
    {
        var (context, scheduler) = CreateJobExecutionContext();
        _retryScheduler.SetupRetry(isRetryable: false);
        _workflowRuntime.SetupCreateClientThrows(new TimeoutException());

        await _job.Execute(context);

        _retryScheduler.Verify(x => x.ScheduleRetryAsync(It.IsAny<IJobExecutionContext>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Never);
        _logger.VerifyLogged(LogLevel.Error, Times.Once());
        scheduler.VerifyDeleted();
    }

    [Theory]
    [InlineData(typeof(InvalidOperationException), null)]
    [InlineData(typeof(ArgumentException), null)]
    [InlineData(typeof(InvalidOperationException), "ResumeWorkflowJob")]
    [InlineData(typeof(ArgumentException), "ResumeWorkflowJob")]
    public async Task Execute_NonTransientException_DeletesJob(Type exceptionType, string? jobKeyName)
    {
        var (context, scheduler) = CreateJobExecutionContext(jobKeyName: jobKeyName);
        _retryScheduler.SetupRetry(isRetryable: false);
        _workflowRuntime.SetupCreateClientThrows((Exception)Activator.CreateInstance(exceptionType)!);

        await _job.Execute(context);

        if (string.IsNullOrEmpty(jobKeyName))
            scheduler.VerifyDeleted();
        else
            scheduler.VerifyNotDeleted();
    }

    [Fact]
    public async Task Execute_TenantFinderThrowsTransientException_SchedulesRetryAndDoesNotPropagate()
    {
        var (context, _) = CreateJobExecutionContext(withTenantId: true);
        _retryScheduler.SetupRetry(isRetryable: true);
        _tenantFinder.Setup(f => f.FindByIdAsync("tenant-123", It.IsAny<CancellationToken>())).ThrowsAsync(new TimeoutException());

        await _job.Execute(context);

        _retryScheduler.VerifyRetryScheduled(context);
        _workflowRuntime.Verify(r => r.CreateClientAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_UsesCorrectWorkflowInstanceId()
    {
        var (context, _) = CreateJobExecutionContext();
        var workflowClient = new Mock<IWorkflowClient>();
        workflowClient.Setup(c => c.RunInstanceAsync(It.IsAny<RunWorkflowInstanceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RunWorkflowInstanceResponse());

        string? capturedInstanceId = null;
        _workflowRuntime.Setup(r => r.CreateClientAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((id, _) => capturedInstanceId = id)
            .ReturnsAsync(workflowClient.Object);

        await _job.Execute(context);

        Assert.Equal("workflow-instance-123", capturedInstanceId);
    }

    [Fact]
    public async Task Execute_WithActivityHandle_DeserializesCorrectly()
    {
        var activityHandle = new ActivityHandle { ActivityId = "activity-123" };
        var serializedHandle = "{\"ActivityId\":\"activity-123\"}";
        var (context, _) = CreateJobExecutionContext(serializedHandle);

        _jsonSerializer.Setup(j => j.Deserialize<ActivityHandle>(serializedHandle))
            .Returns(activityHandle);

        var workflowClient = new Mock<IWorkflowClient>();
        RunWorkflowInstanceRequest? capturedRequest = null;
        workflowClient.Setup(c => c.RunInstanceAsync(It.IsAny<RunWorkflowInstanceRequest>(), It.IsAny<CancellationToken>()))
            .Callback<RunWorkflowInstanceRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new RunWorkflowInstanceResponse());

        _workflowRuntime.Setup(r => r.CreateClientAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflowClient.Object);

        await _job.Execute(context);

        Assert.NotNull(capturedRequest);
        Assert.Equal("activity-123", capturedRequest.ActivityHandle?.ActivityId);
    }

    private static (IJobExecutionContext, Mock<QuartzScheduler>) CreateJobExecutionContext(string? activityHandle = null,
        string? jobKeyName = null, bool withTenantId = false)
    {
        var jobData = new Dictionary<string, object>
        {
            { "WorkflowInstanceId", "workflow-instance-123" },
            { "BookmarkId", "bookmark-123" }
        };

        if (activityHandle != null)
            jobData.Add("ActivityHandle", activityHandle);

        if (withTenantId)
            jobData["TenantId"] = "tenant-123";

        return QuartzJobTestHelper.CreateJobExecutionContext(jobData, jobKeyName);
    }
}
