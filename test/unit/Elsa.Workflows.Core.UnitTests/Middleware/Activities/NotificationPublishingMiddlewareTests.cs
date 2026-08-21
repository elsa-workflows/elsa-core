using Elsa.Workflows.Middleware.Activities;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Pipelines.ActivityExecution;
using NSubstitute;
using Xunit.Abstractions;

namespace Elsa.Workflows.Core.UnitTests.Middleware.Activities;

public class NotificationPublishingMiddlewareTests : ActivityExecutionMiddlewareTestsBase<NotificationPublishingMiddleware>
{
    public NotificationPublishingMiddlewareTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        PipelineFactory += b => b.UseMiddleware<DefaultActivityInvokerMiddleware>();
    }

    [Fact]
    public async Task InvokeAsync_Raises_ActivityExecutingEvent()
    {
        // Act
        await Pipeline.ExecuteAsync(ExecutionContext);

        // Assert
        await _notificationSender.Received().SendAsync(Arg.Any<ActivityExecuting>());
    }

    [Fact]
    public async Task InvokeAsync_Raises_ActivityExecutedEvent()
    {
        // Act
        await Pipeline.ExecuteAsync(ExecutionContext);

        // Assert
        await _notificationSender.Received().SendAsync(Arg.Any<ActivityExecuted>());
    }
}
