// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// Required for JSON serialization configured with reference handling.
namespace Elsa.Workflows.Core.State;

// Can't use records when using System.Text.Json serialization and reference handling. Hence, using a class with default constructor.
//public record CompletionCallbackState(string OwnerId, string ChildId, string MethodName);

/// <summary>
/// Represents a serializable completion callback that is registered by an activity.
/// </summary>
public class CompletionCallbackState
{
    // ReSharper disable once UnusedMember.Global
    // Required for JSON serialization configured with reference handling.
    /// <summary>
    /// Creates a new instance of the <see cref="CompletionCallbackState"/> class.
    /// </summary>
    public CompletionCallbackState()
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="CompletionCallbackState"/> class.
    /// </summary>
    /// <param name="ownerInstanceId">The ID of the activity instance that registered the callback.</param>
    /// <param name="childNodeId">The ID of the child node that the callback is registered for.</param>
    /// <param name="methodName">The name of the method to invoke when the child node completes.</param>
    /// <param name="tag">An optional tag.</param>
    public CompletionCallbackState(string ownerInstanceId, string childNodeId, string? methodName, object? tag = default)
    {
        OwnerInstanceId = ownerInstanceId;
        ChildNodeId = childNodeId;
        MethodName = methodName;
        Tag = tag;
    }

    /// <summary>
    /// Gets the ID of the activity instance that registered the callback.
    /// </summary>
    public string OwnerInstanceId { get; init; } = default!;
    
    /// <summary>
    /// Gets the ID of the child node that the callback is registered for.
    /// </summary>
    public string ChildNodeId { get; init; } = default!;
    
    /// <summary>
    /// Gets the name of the method to invoke when the child node completes.
    /// </summary>
    public string? MethodName { get; init; }
    
    /// <summary>
    /// Gets the tag.
    /// </summary>
    public object? Tag { get; init; }
}