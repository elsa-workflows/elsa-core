using Elsa.Workflows.Runtime.Messages;

namespace Elsa.Workflows.Runtime;

public interface IBookmarkInvoker
{
    Task<RunWorkflowInstanceResponse> InvokeAsync(InvokeBookmarkRequest request, CancellationToken cancellationToken = default);
}