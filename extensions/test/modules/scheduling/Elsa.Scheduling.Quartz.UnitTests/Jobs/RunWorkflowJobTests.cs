using Elsa.Common.Multitenancy;
using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Scheduling.Quartz.Jobs;
using Elsa.Scheduling.Quartz.UnitTests.Helpers;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using QuartzScheduler = Quartz.IScheduler;

namespace Elsa.Scheduling.Quartz.UnitTests.Jobs;

public class RunWorkflowJobTests
{
    private readonly Mock<ITenantAccessor> _tenantAccessor = QuartzJobTestHelper.CreateTenantAccessor();
    private readonly Mock<ITenantFinder> _tenantFinder = new();
    private readonly Mock<IWorkflowStarter> _workflowStarter = new();
    private readonly Mock<IQuartzJobRetryScheduler> _retryScheduler = new();
    private readonly Mock<ILogger<RunWorkflowJob>> _logger = new();
    private readonly RunWorkflowJob _job;

    public RunWorkflowJobTests()
    {
        _job = new(
            _tenantAccessor.Object,
            _tenantFinder.Object,
            _workflowStarter.Object,
            _retryScheduler.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Execute_SuccessfulStart_CallsWorkflowStarter()
    {
        var (context, _) = CreateJobExecutionContext();
        _workflowStarter.SetupStartWorkflow(new() { WorkflowInstanceId = "workflow-123" });

        await _job.Execute(context);

        _workflowStarter.VerifyStartWorkflowCalled();
    }

    [Fact]
    public async Task Execute_CannotStart_LogsWarningAndReturns()
    {
        var (context, _) = CreateJobExecutionContext();
        _workflowStarter.SetupStartWorkflow(new() { CannotStart = true });

        await _job.Execute(context);

        _workflowStarter.VerifyStartWorkflowCalled();
    }

    [Fact]
    public async Task Execute_WorkflowGraphNotFound_DeletesJob()
    {
        var (context, scheduler) = CreateJobExecutionContext();
        var handle = WorkflowDefinitionHandle.ByDefinitionVersionId("workflow-def-123");
        _workflowStarter.SetupStartWorkflowThrows(new WorkflowGraphNotFoundException("Not found", handle));

        await _job.Execute(context);

        scheduler.VerifyUnscheduled();
    }

    [Fact]
    public async Task Execute_WorkflowGraphNotFoundOnStaleOriginal_DoesNotUnscheduleReplacementOriginal()
    {
        var jobData = new Dictionary<string, object>
        {
            ["DefinitionVersionId"] = "workflow-def-123"
        };
        var (context, scheduler) = QuartzJobTestHelper.CreateJobExecutionContext(
            jobData,
            triggerName: "task-1",
            triggerData: new Dictionary<string, object>
            {
                [QuartzJobDataKeys.RetryScheduleGeneration] = "old-generation"
            });
        var handle = WorkflowDefinitionHandle.ByDefinitionVersionId("workflow-def-123");
        _workflowStarter.SetupStartWorkflowThrows(new WorkflowGraphNotFoundException("Not found", handle));
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
    public async Task Execute_WorkflowGraphNotFoundOnRetry_UnschedulesTheOriginalScheduleToo()
    {
        var jobData = new Dictionary<string, object>
        {
            ["DefinitionVersionId"] = "workflow-def-123",
        };
        var (context, scheduler) = QuartzJobTestHelper.CreateJobExecutionContext(
            jobData,
            triggerName: "task-retry-retry",
            triggerData: new Dictionary<string, object>
            {
                [QuartzJobDataKeys.RetryTrigger] = bool.TrueString,
                [QuartzJobDataKeys.RetryOriginalTriggerName] = "task-retry",
                [QuartzJobDataKeys.RetryOriginalTriggerGroup] = new TriggerKey("task-retry").Group
            });
        var handle = WorkflowDefinitionHandle.ByDefinitionVersionId("workflow-def-123");
        _workflowStarter.SetupStartWorkflowThrows(new WorkflowGraphNotFoundException("Not found", handle));
        var originalTrigger = TriggerBuilder.Create()
            .WithIdentity("task-retry")
            .ForJob(new JobKey("test-job"))
            .Build();
        scheduler.Setup(s => s.GetTrigger(new TriggerKey("task-retry"), It.IsAny<CancellationToken>())).ReturnsAsync(originalTrigger);
        var retryTrigger = TriggerBuilder.Create()
            .WithIdentity(QuartzTriggerKeys.GetRetryTriggerKey(originalTrigger.Key))
            .ForJob(new JobKey("test-job"))
            .Build();
        scheduler.Setup(s => s.GetTrigger(retryTrigger.Key, It.IsAny<CancellationToken>())).ReturnsAsync(retryTrigger);

        await _job.Execute(context);

        scheduler.Verify(s => s.UnscheduleJob(retryTrigger.Key, It.IsAny<CancellationToken>()), Times.Once);
        scheduler.Verify(s => s.UnscheduleJob(new TriggerKey("task-retry"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_WorkflowGraphNotFoundOnStaleRetry_DoesNotUnscheduleReplacementOriginal()
    {
        var jobData = new Dictionary<string, object>
        {
            ["DefinitionVersionId"] = "workflow-def-123",
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
        _workflowStarter.SetupStartWorkflowThrows(new WorkflowGraphNotFoundException("Not found", handle));
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
    [InlineData(typeof(TimeoutException))]
    [InlineData(typeof(HttpRequestException))]
    public async Task Execute_RetryableException_ReschedulesJob(Type exceptionType)
    {
        var (context, scheduler) = CreateJobExecutionContext();
        _retryScheduler.SetupRetry(isRetryable: true);
        _workflowStarter.SetupStartWorkflowThrows((Exception)Activator.CreateInstance(exceptionType)!);

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
        _workflowStarter.SetupStartWorkflowThrows(new TimeoutException());

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
        _workflowStarter.SetupStartWorkflowThrows(new TimeoutException());

        await _job.Execute(context);

        _retryScheduler.Verify(x => x.ScheduleRetryAsync(It.IsAny<IJobExecutionContext>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Never);
        _logger.VerifyLogged(LogLevel.Error, Times.Once());
        scheduler.VerifyDeleted();
    }

    [Theory]
    [InlineData(typeof(InvalidOperationException), null)]
    [InlineData(typeof(ArgumentException), null)]
    [InlineData(typeof(InvalidOperationException), "RunWorkflowJob")]
    [InlineData(typeof(ArgumentException), "RunWorkflowJob")]
    public async Task Execute_NonTransientException_DeletesJob(Type exceptionType, string? jobKeyName)
    {
        var (context, scheduler) = CreateJobExecutionContext(jobKeyName);
        _retryScheduler.SetupRetry(isRetryable: false);
        _workflowStarter.SetupStartWorkflowThrows((Exception)Activator.CreateInstance(exceptionType)!);

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
        _workflowStarter.Verify(w => w.StartWorkflowAsync(It.IsAny<StartWorkflowRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_UsesCorrectWorkflowDefinitionHandle()
    {
        var (context, _) = CreateJobExecutionContext();
        StartWorkflowRequest? capturedRequest = null;
        _workflowStarter.Setup(w => w.StartWorkflowAsync(It.IsAny<StartWorkflowRequest>(), It.IsAny<CancellationToken>()))
            .Callback<StartWorkflowRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new StartWorkflowResponse { WorkflowInstanceId = "workflow-123" });

        await _job.Execute(context);

        Assert.NotNull(capturedRequest);
        Assert.Equal("workflow-def-123", capturedRequest.WorkflowDefinitionHandle.DefinitionVersionId);
    }

    private static (IJobExecutionContext, Mock<QuartzScheduler>) CreateJobExecutionContext(string? jobKeyName = null, bool withTenantId = false)
    {
        var jobData = new Dictionary<string, object>
        {
            { "DefinitionVersionId", "workflow-def-123" },
            { "CorrelationId", "corr-123" },
            { "TriggerActivityId", "trigger-123" }
        };

        if (withTenantId)
            jobData["TenantId"] = "tenant-123";

        return QuartzJobTestHelper.CreateJobExecutionContext(jobData, jobKeyName);
    }
}
