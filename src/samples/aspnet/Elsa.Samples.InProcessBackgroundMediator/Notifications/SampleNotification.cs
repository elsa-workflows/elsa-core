using MediatR;

namespace Elsa.Samples.InProcessBackgroundMediator.Notifications;

public record SampleNotification(string Message) : INotification;