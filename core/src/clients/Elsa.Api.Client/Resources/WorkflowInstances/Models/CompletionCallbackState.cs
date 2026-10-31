using System.Text.Json.Serialization;

namespace Elsa.Api.Client.Resources.WorkflowInstances.Models;

/// <summary>
/// Represents a completion callback state.
/// </summary>
public class CompletionCallbackState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompletionCallbackState"/> class.
    /// </summary>
    [JsonConstructor]
    public CompletionCallbackState()
    {
    }

    /// <summary>
    /// The ID of the activity instance that owns the callback.
    /// </summary>
    public string OwnerInstanceId { get; init; } = default!;
    
    /// <summary>
    /// The ID of the child activity node.
    /// </summary>
    public string ChildNodeId { get; init; } = default!;
    
    /// <summary>
    /// The name of the method to invoke on the owner when the child completes.
    /// </summary>
    public string? MethodName { get; init; } = default!;
}