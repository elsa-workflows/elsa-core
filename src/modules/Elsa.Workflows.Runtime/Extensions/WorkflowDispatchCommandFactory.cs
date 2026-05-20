using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Workflows.Runtime;

internal static class WorkflowDispatchCommandFactory
{
    public static DispatchWorkflowDefinitionCommand CreateCommand(DispatchWorkflowDefinitionRequest request, string? instanceId = null)
    {
        var useGeneratedInstanceId = string.IsNullOrWhiteSpace(request.InstanceId);

        return new(request.DefinitionVersionId)
        {
            Input = request.Input,
            Properties = request.Properties,
            CorrelationId = request.CorrelationId,
            InstanceId = useGeneratedInstanceId ? instanceId : request.InstanceId,
            TriggerActivityId = request.TriggerActivityId,
            ParentWorkflowInstanceId = request.ParentWorkflowInstanceId,
            SchedulingActivityExecutionId = request.SchedulingActivityExecutionId,
            SchedulingWorkflowInstanceId = request.SchedulingWorkflowInstanceId,
            SchedulingCallStackDepth = request.SchedulingCallStackDepth,
            SkipIfInstanceExists = useGeneratedInstanceId
        };
    }

    public static DispatchWorkflowInstanceCommand CreateCommand(DispatchWorkflowInstanceRequest request)
    {
        return new(request.InstanceId)
        {
            BookmarkId = request.BookmarkId,
            ActivityHandle = request.ActivityHandle,
            Input = request.Input,
            Properties = request.Properties,
            CorrelationId = request.CorrelationId
        };
    }

    public static DispatchTriggerWorkflowsCommand CreateCommand(DispatchTriggerWorkflowsRequest request)
    {
        return new(request.ActivityTypeName, request.BookmarkPayload)
        {
            CorrelationId = request.CorrelationId,
            WorkflowInstanceId = request.WorkflowInstanceId,
            ActivityInstanceId = request.ActivityInstanceId,
            Input = request.Input,
            Properties = request.Properties
        };
    }

    public static DispatchResumeWorkflowsCommand CreateCommand(DispatchResumeWorkflowsRequest request)
    {
        return new(request.ActivityTypeName, request.BookmarkPayload)
        {
            CorrelationId = request.CorrelationId,
            WorkflowInstanceId = request.WorkflowInstanceId,
            ActivityInstanceId = request.ActivityInstanceId,
            Input = request.Input
        };
    }
}
