using Elsa.Workflows.Exceptions;

namespace Elsa.Workflows.Runtime.Responses;

/// <summary>
/// Represents the response of a dispatch action for a workflow definition.
/// </summary>
public record DispatchWorkflowResponse(FaultException? Fault)
{
    /// <summary>
    /// Creates a response indicating that the dispatch action was successful.
    /// </summary>
    /// <returns></returns>
    public static DispatchWorkflowResponse Success() => new(default(FaultException?));

    /// <summary>
    /// Creates a response indicating that the specified channel does not exist.
    /// </summary>
    public static DispatchWorkflowResponse UnknownChannel() => new(new FaultException(RuntimeFaultCodes.UnknownChannel, RuntimeFaultCategories.Dispatch, DefaultFaultTypes.System, "The specified channel does not exist."));

    /// <summary>
    /// Gets a value indicating whether the dispatch of a workflow definition succeeded.
    /// </summary>
    /// <value>
    /// <c>true</c> if the dispatch succeeded; otherwise, <c>false</c>.
    /// </value>
    public bool Succeeded => Fault == null;
    
    /// <summary>
    /// Throws an exception if the dispatch failed.
    /// </summary>
    public void ThrowIfFailed()
    {
        if (Fault != null)
            throw Fault;
    }
}