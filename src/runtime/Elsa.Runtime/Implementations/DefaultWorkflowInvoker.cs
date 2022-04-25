using Elsa.Models;
using Elsa.Runtime.Models;
using Elsa.Runtime.Services;

namespace Elsa.Runtime.Implementations;

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