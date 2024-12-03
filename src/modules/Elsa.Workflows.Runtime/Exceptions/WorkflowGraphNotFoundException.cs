using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime.Exceptions;

public class WorkflowGraphNotFoundException(string message, WorkflowDefinitionHandle workflowDefinitionHandle) : Exception(message)
{
    public WorkflowDefinitionHandle WorkflowDefinitionHandle { get; } = workflowDefinitionHandle;
}