using Elsa.Mediator.Contracts;
using Elsa.Workflows.Notifications;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ActivityNotificationsMiddleware;

public class TestHandler : INotificationHandler<ActivityExecuting>, INotificationHandler<ActivityExecuted>
{
    private readonly Spy _spy;

    public TestHandler(Spy spy)
    {
        _spy = spy;
    }
    
    public Task HandleAsync(ActivityExecuting notification, CancellationToken cancellationToken)
    {
        _spy.ActivityExecutingWasCalled = true;
        return Task.CompletedTask;
    }

    public Task HandleAsync(ActivityExecuted notification, CancellationToken cancellationToken)
    {
        _spy.ActivityExecutedWasCalled = true;
        return Task.CompletedTask;
    }
}