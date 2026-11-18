using Elsa.Extensions;
using Elsa.Workflows.Management.Exceptions;

namespace Elsa.Workflows.Runtime.ProtoActor;

internal static class WorkflowInstanceRpcErrors
{
    private const string InstanceAlreadyExists = "elsa.workflow-instance-already-exists";

    public static string FromCreateFailure(Task task)
    {
        if (task.Exception?.GetBaseException() is WorkflowInstanceAlreadyExistsException)
            return InstanceAlreadyExists;

        return task.Exception?.Message ?? "The workflow instance creation was canceled.";
    }

    public static Exception ToException(string actorId, string error)
    {
        return error == InstanceAlreadyExists
            ? new WorkflowInstanceAlreadyExistsException(ClusterExtensions.GetWorkflowInstanceId(actorId))
            : new Exception(error);
    }
}
