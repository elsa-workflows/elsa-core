using System.ComponentModel;
using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.MassTransit.Implementations;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Models;

namespace Elsa.MassTransit.Activities;

/// <summary>
/// A generic activity that waits for a message of a given type to be received. Used by the <see cref="MassTransitActivityTypeProvider"/>.
/// </summary>
[Browsable(false)]
public class MessageReceived : Trigger<object>
{
    internal const string InputKey = "Message";

    /// <inheritdoc />
    [JsonConstructor]
    public MessageReceived()
    {
    }

    /// <summary>
    /// The message type to receive.
    /// </summary>
    public Type MessageType { get; set; } = default!;

    /// <inheritdoc />
    protected override object GetTriggerPayload(TriggerIndexingContext context) => GetBookmarkPayload(context.ExpressionExecutionContext);

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // If we did not receive external input, it means we are just now encountering this activity and we need to block execution by creating a bookmark.
        if (!context.TryGetInput<object>(InputKey, out var message))
        {
            // Create bookmarks for when we receive the expected HTTP request.
            context.CreateBookmark(GetBookmarkPayload(context.ExpressionExecutionContext));
            return;
        }

        // Provide the received message as output.
        context.Set(Result, message);
        
        // Complete.
        await context.CompleteActivityAsync();
    }

    private object GetBookmarkPayload(ExpressionExecutionContext context)
    {
        // Generate bookmark data for message type.
        return new MessageReceivedBookmarkPayload(MessageType);
    }
}

internal record MessageReceivedBookmarkPayload(Type MessageType);