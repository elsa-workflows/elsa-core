namespace Elsa.Workflows.Management.Services;

public class WorkflowInstanceVariableManager(
    IWorkflowInstanceManager workflowInstanceManager, 
    IWorkflowDefinitionService workflowDefinitionService, 
    IServiceProvider serviceProvider, 
    IWorkflowInstanceVariableReader variableReader,
    IWorkflowInstanceVariableWriter variableWriter) : IWorkflowInstanceVariableManager
{
    public async Task<IEnumerable<ResolvedVariable>> GetVariablesAsync(string workflowInstanceId, IEnumerable<string>? excludeTags = default, CancellationToken cancellationToken = default)
    {
        var workflowExecutionContext = await GetWorkflowExecutionContextAsync(workflowInstanceId, cancellationToken);
        if (workflowExecutionContext == null) return [];
        return await variableReader.GetVariables(workflowExecutionContext, excludeTags, cancellationToken);
    }

    public Task<IEnumerable<ResolvedVariable>> GetVariablesAsync(WorkflowExecutionContext workflowExecutionContext, IEnumerable<string>? excludeTags = default, CancellationToken cancellationToken = default)
    {
        return variableReader.GetVariables(workflowExecutionContext, excludeTags, cancellationToken);
    }

    public async Task<IEnumerable<ResolvedVariable>> SetVariablesAsync(string workflowInstanceId, IEnumerable<VariableUpdateValue> variables, CancellationToken cancellationToken = default)
    {
        var workflowExecutionContext = await GetWorkflowExecutionContextAsync(workflowInstanceId, cancellationToken);
        if (workflowExecutionContext == null) return [];
        var resolvedVariables = await variableWriter.SetVariables(workflowExecutionContext, variables, cancellationToken);
        await workflowInstanceManager.SaveAsync(workflowExecutionContext, cancellationToken);
        return resolvedVariables;
    }

    public Task<IEnumerable<ResolvedVariable>> SetVariablesAsync(WorkflowExecutionContext workflowExecutionContext, IEnumerable<VariableUpdateValue> variables, CancellationToken cancellationToken = default)
    {
        return variableWriter.SetVariables(workflowExecutionContext, variables, cancellationToken);
    }
    
    private async Task<WorkflowExecutionContext?> GetWorkflowExecutionContextAsync(string workflowInstanceId, CancellationToken cancellationToken)
    {
        var workflowInstance = await workflowInstanceManager.FindByIdAsync(workflowInstanceId, cancellationToken);

        if (workflowInstance == null)
            return null;
        
        var workflowState = workflowInstance.WorkflowState;
        var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(workflowState.DefinitionVersionId, cancellationToken);
        
        if (workflowGraph == null)
            return null;
        
        return await WorkflowExecutionContext.CreateAsync(
            serviceProvider,
            workflowGraph,
            workflowState,
            cancellationToken: cancellationToken);
    }
}