using Elsa.Extensions;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Notifications;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Activities;

/// <summary>
/// Notifies the application that a domain event with a given name is requested to be published.
/// </summary>
[Activity("Elsa", "Primitives", "Requests a given domain event to be published. ", Kind = ActivityKind.Action)]
[UsedImplicitly]
public class DomainEvent: Activity
{
    /// <summary>
    /// The name of the domain event being published.
    /// </summary>
    [Input(Description = "The name of the domain event being published.")]
    public Input<string> DomainEventName { get; set; } = default!;

    /// <summary>
    /// The payload of the domain event being published.
    /// </summary>
    [Input(Description = "Any additional parameters to send to the domain event.")]
    public Input<object?> Payload { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var domainEventName = DomainEventName.Get(context);
        var identityGenerator = context.GetRequiredService<IIdentityGenerator>();
        var domainEventId = identityGenerator.GenerateId();

        // Publish the domain event
        var domainEventPayload = Payload.GetOrDefault(context);
        var domainEventNotification = new DomainEventNotification(context, domainEventId, domainEventName, domainEventPayload);
        var dispatcher = context.GetRequiredService<IDomainEventDispatcher>();

        await dispatcher.DispatchAsync(domainEventNotification, context.CancellationToken);
        await context.CompleteActivityAsync();
    }
}