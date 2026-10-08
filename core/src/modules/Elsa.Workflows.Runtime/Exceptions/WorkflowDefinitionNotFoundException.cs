using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime.Exceptions;

public class WorkflowDefinitionNotFoundException(string message, WorkflowDefinitionHandle workflowDefinitionHandle) : Exception(message)
{
    public WorkflowDefinitionHandle WorkflowDefinitionHandle { get; } = workflowDefinitionHandle;
}