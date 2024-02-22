using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Workflows.Runtime.Commands;

/// <summary>
/// Sends a command to cancel workflows.
/// </summary>
public class CancelWorkflowsCommand : ICommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CancelWorkflowsCommand"/> class.
    /// </summary>
    public CancelWorkflowsCommand()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CancelWorkflowsCommand"/> class.
    /// </summary>
    public CancelWorkflowsCommand(DispatchCancelWorkflowsRequest request)
    {
        Request = request;
    }
    
    /// <summary>
    /// Gets or sets the request.
    /// </summary>
    public DispatchCancelWorkflowsRequest Request { get; set; } = default!;
}