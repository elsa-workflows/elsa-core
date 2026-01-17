using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime.Exceptions;

public class WorkflowMaterializerNotFoundException(string message, WorkflowDefinitionHandle workflowDefinitionHandle) : Exception(message)
{
    public WorkflowDefinitionHandle WorkflowDefinitionHandle { get; } = workflowDefinitionHandle;
}