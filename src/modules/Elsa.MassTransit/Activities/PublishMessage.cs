using System.ComponentModel;
using System.Text.Json.Serialization;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.MassTransit.Implementations;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using MassTransit;
using MassTransit.Middleware;

namespace Elsa.MassTransit.Activities;

/// <summary>
/// A generic activity that publishes a message of a given type. Used by the <see cref="MassTransitActivityTypeProvider"/>.
/// </summary>
[Browsable(false)]
public class PublishMessage : Activity
{
    /// <inheritdoc />
    [JsonConstructor]
    public PublishMessage()
    {
    }

    /// <summary>
    /// The message type to publish.
    /// </summary>
    public Type MessageType { get; set; } = default!;

    /// <summary>
    /// The message to send. Must be a concrete implementation of the configured <see cref="MessageType"/>.
    /// </summary>
    [Input(Description = "The message to send. Must be a concrete implementation of the configured message type.")]
    public Input<object> Message { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var bus = context.GetRequiredService<IBus>();
        var message = Message.Get(context).ConvertTo(MessageType)!;
        await bus.Publish(message, context.CancellationToken);
    }
}