using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.MassTransit.Services;
using Elsa.Workflows;

namespace Elsa.MassTransit.Activities;

/// <summary>
/// A generic activity that waits for a message of a given type to be received. Used by the <see cref="MassTransitActivityTypeProvider"/>.
/// </summary>
[Browsable(false)]
public class MessageReceived : Trigger<object>
{
    internal const string InputKey = "Message";

    /// <inheritdoc />
    public MessageReceived([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
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
        if (!context.TryGetWorkflowInput<object>(InputKey, out var message) || message.GetType() != MessageType)
        {
            // Create bookmarks for when we receive the expected HTTP request.
            context.CreateBookmark(GetBookmarkPayload(context.ExpressionExecutionContext), ResumeAsync, includeActivityInstanceId: false);
            return;
        }

        // Provide the received message as output.
        context.Set(Result, message);
        
        // Remove the input to prevent it from being passed to the next activity.
        context.WorkflowInput.Remove(InputKey);
        
        // Complete.
        await context.CompleteActivityAsync();
    }

    private ValueTask ResumeAsync(ActivityExecutionContext context)
    {
        // Remove the input to prevent it from being passed to the next activity.
        context.WorkflowInput.Remove(InputKey);
        
        // Complete.
        return context.CompleteActivityAsync();
    }

    private object GetBookmarkPayload(ExpressionExecutionContext context)
    {
        // Generate bookmark data for message type.
        return new MessageReceivedBookmarkPayload(MessageType);
    }
}

internal record MessageReceivedBookmarkPayload(Type MessageType);