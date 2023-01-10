using Elsa.Samples.InProcessBackgroundMediator.Notifications;
using MediatR;

namespace Elsa.Samples.InProcessBackgroundMediator.Handlers;

/// <summary>
/// A sample notification handler that takes some time to execute.
/// </summary>
public class SampleNotificationHandler : INotificationHandler<SampleNotification>
{
    public async Task Handle(SampleNotification notification, CancellationToken cancellationToken)
    {
        // Simulate a slow process.
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        
        // Perform "the work".
        Console.WriteLine(notification.Message);
    }
}