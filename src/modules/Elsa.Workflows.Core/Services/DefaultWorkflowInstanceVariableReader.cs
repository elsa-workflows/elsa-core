namespace Elsa.Workflows;

public class DefaultWorkflowInstanceVariableReader(IVariablePersistenceManager variablePersistenceManager) : IWorkflowInstanceVariableReader
{
    public async Task<IEnumerable<ResolvedVariable>> GetVariables(WorkflowExecutionContext workflowExecutionContext, IEnumerable<string>? excludeTags = default, CancellationToken cancellationToken = default)
    {
        var workflow = workflowExecutionContext.Workflow;
        var workflowVariables = workflow.Variables;
        var rootWorkflowActivityExecutionContext = workflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => x.ParentActivityExecutionContext == null);
        
        if (rootWorkflowActivityExecutionContext == null) 
            return [];
        
        await variablePersistenceManager.LoadVariablesAsync(workflowExecutionContext, excludeTags);
        var resolvedVariables = new List<ResolvedVariable>();

        foreach (var workflowVariable in workflowVariables)
        {
            var value = workflowVariable.Get(rootWorkflowActivityExecutionContext.ExpressionExecutionContext);
            resolvedVariables.Add(new ResolvedVariable(workflowVariable, value));
        }
        
        return resolvedVariables;
    }
}