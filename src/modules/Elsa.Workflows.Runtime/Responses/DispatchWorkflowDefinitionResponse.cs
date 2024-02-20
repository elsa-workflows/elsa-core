namespace Elsa.Workflows.Runtime.Responses;

/// <summary>
/// Represents the response of a dispatch action for a workflow definition.
/// </summary>
public record DispatchWorkflowResponse(bool Succeeded, string? ErrorMessage)
{
    /// <summary>
    /// Creates a response indicating that the dispatch action was successful.
    /// </summary>
    /// <returns></returns>
    public static DispatchWorkflowResponse Success() => new DispatchWorkflowResponse(true, default);
    
    /// <summary>
    /// Creates a response indicating that the specified channel does not exist.
    /// </summary>
    public static DispatchWorkflowResponse UnknownChannel() => new DispatchWorkflowResponse(false, "The specified channel does not exist.");
}