using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Requests;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class WorkflowValidator : IWorkflowValidator
{
    private readonly IRequestSender _requestSender;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowValidator"/> class.
    /// </summary>
    public WorkflowValidator(IRequestSender requestSender)
    {
        _requestSender = requestSender;
    }
    
    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowValidationError>> ValidateAsync(Workflow workflow, CancellationToken cancellationToken = default)
    {
        var responses = await _requestSender.SendAsync(new ValidateWorkflowRequest(workflow), cancellationToken);
        return responses.SelectMany(r => r.ValidationErrors).ToList();
    }
}