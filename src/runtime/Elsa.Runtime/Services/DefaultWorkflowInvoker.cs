using Elsa.Models;
using Elsa.Runtime.Contracts;
using Elsa.Runtime.Models;

namespace Elsa.Runtime.Services;

/// <summary>
/// A basic implementation that directly executes the specified workflow in local memory (as opposed elsewhere in some cluster).
/// </summary>
public class DefaultWorkflowInvoker : IWorkflowInvoker
{
    public Task<InvokeWorkflowResult> InvokeAsync(InvokeWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<InvokeWorkflowResult> InvokeAsync(InvokeWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}