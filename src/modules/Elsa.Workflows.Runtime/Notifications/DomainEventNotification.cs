using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Runtime.Notifications;

/// <summary>
/// A domain event that applications can subscribe to.
/// </summary>
/// <param name="ActivityExecutionContext">The context of the activity that requested the domain event to be published.</param>
/// <param name="DomainEventId">A unique identifier for an individual domain event notification.</param>
/// <param name="DomainEventName">The name of the domain event requested to be published.</param>
/// <param name="DomainEventPayload">Any additional parameters to send to the domain event.</param>
public record DomainEventNotification(ActivityExecutionContext ActivityExecutionContext, string DomainEventId, string DomainEventName, object? DomainEventPayload) : INotification;