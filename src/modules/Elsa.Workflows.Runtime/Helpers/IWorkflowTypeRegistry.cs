namespace Elsa.Workflows.Runtime.Helpers;

internal interface IWorkflowTypeRegistry
{
    IEnumerable<Type> WorkflowTypes { get; }

    void AddWorkflowType(Type workflowType);
}
