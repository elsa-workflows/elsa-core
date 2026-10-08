namespace Elsa.Workflows;

public class DefaultWorkflowInstanceVariableWriter(IVariablePersistenceManager variablePersistenceManager) : IWorkflowInstanceVariableWriter
{
    public async Task<IEnumerable<ResolvedVariable>> SetVariables(WorkflowExecutionContext workflowExecutionContext, IEnumerable<VariableUpdateValue> variables, CancellationToken cancellationToken = default)
    {
        var workflow = workflowExecutionContext.Workflow;
        var workflowVariables = workflow.Variables;
        var rootWorkflowActivityExecutionContext = workflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => x.ParentActivityExecutionContext == null);

        if (rootWorkflowActivityExecutionContext == null)
            return [];

        await variablePersistenceManager.LoadVariablesAsync(workflowExecutionContext);
        var variablesToUpdate = variables.ToDictionary(x => x.Id, x => x.Value);
        var resolvedVariables = new Dictionary<string, ResolvedVariable>();

        foreach (var workflowVariable in workflowVariables)
        {
            var currentValue = workflowVariable.Get(rootWorkflowActivityExecutionContext.ExpressionExecutionContext);
            var resolvedVariable = new ResolvedVariable(workflowVariable, currentValue);
            resolvedVariables[workflowVariable.Id] = resolvedVariable;

            if (!variablesToUpdate.TryGetValue(workflowVariable.Id, out var value))
                continue;

            workflowVariable.Set(rootWorkflowActivityExecutionContext.ExpressionExecutionContext, value);
            resolvedVariable = resolvedVariable with
            {
                Value = value
            };

            resolvedVariables[workflowVariable.Id] = resolvedVariable;
        }

        await variablePersistenceManager.SaveVariablesAsync(workflowExecutionContext);
        return resolvedVariables.Values;
    }
}