using Elsa.Workflows.Runtime.Messages;

namespace Elsa.Workflows.Runtime;

public class BookmarkInvoker(IWorkflowRuntime workflowRuntime) : IBookmarkInvoker
{
    public async Task<RunWorkflowInstanceResponse> InvokeAsync(InvokeBookmarkRequest request, CancellationToken cancellationToken = default)
    {
        var runRequest = new RunWorkflowInstanceRequest
        {
            Input = request.Input,
            Properties = request.Properties,
            ActivityHandle = request.ActivityHandle,
            BookmarkId = request.BookmarkId
        };
        
        var workflowInstanceId = request.WorkflowInstanceId;
        var workflowClient = await workflowRuntime.CreateClientAsync(workflowInstanceId, cancellationToken);
        return await workflowClient.RunInstanceAsync(runRequest, cancellationToken);
    }
}