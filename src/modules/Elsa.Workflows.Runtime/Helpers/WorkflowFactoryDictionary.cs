namespace Elsa.Workflows.Runtime.Helpers;

internal sealed class WorkflowFactoryDictionary : Dictionary<string, Func<IServiceProvider, ValueTask<IWorkflow>>>, IWorkflowTypeRegistry
{
    private readonly ISet<Type> _workflowTypes = new HashSet<Type>();

    public IEnumerable<Type> WorkflowTypes => _workflowTypes;

    public void AddWorkflowType(Type workflowType)
    {
        WorkflowTypeValidator.Validate(workflowType);
        _workflowTypes.Add(workflowType);
    }
}
