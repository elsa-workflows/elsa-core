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
    public MessageReceived([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <summary>
    /// The message type to receive.
    /// </summary>
    public Type MessageType { get; set; } = null!;

    /// <inheritdoc />
    protected override object GetTriggerPayload(TriggerIndexingContext context) => GetBookmarkPayload(context.ExpressionExecutionContext);

    /// <inheritdoc />
    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // If we did not receive external input, it means we are just now encountering this activity, and we need to block execution by creating a bookmark.
        if (!TryGetMessage(context, out var message))
        {
            // Create bookmarks for when we receive the expected HTTP request.
            context.CreateBookmark(GetBookmarkPayload(context.ExpressionExecutionContext), ResumeAsync, includeActivityInstanceId: false);
            return default;
        }

        return ExecuteInternalAsync(context, message);
    }

    private ValueTask ResumeAsync(ActivityExecutionContext context)
    {
        if(!TryGetMessage(context, out var message))
            throw new InvalidOperationException("Message was not received.");
        return ExecuteInternalAsync(context, message);
    }

    private ValueTask ExecuteInternalAsync(ActivityExecutionContext context, object message)
    {
        // Provide the received message as output.
        context.Set(Result, message);

        // Remove the input to prevent it from being passed to the next activity.
        context.WorkflowInput.Remove(InputKey);

        // Complete.
        return context.CompleteActivityAsync();
    }

    private bool TryGetMessage(ActivityExecutionContext context, out object message)
    {
        if (!context.TryGetWorkflowInput(InputKey, out message))
            return false;

        if (message.GetType() == MessageType)
            return true;

        message = null!;
        return false;
    }

    private object GetBookmarkPayload(ExpressionExecutionContext context)
    {
        // Generate bookmark data for message type.
        return new MessageReceivedBookmarkPayload(MessageType);
    }
}

internal record MessageReceivedBookmarkPayload(Type MessageType);