namespace Elsa.Workflows.Management.Services;

public class WorkflowInstanceVariableManager(
    IWorkflowInstanceManager workflowInstanceManager, 
    IWorkflowDefinitionService workflowDefinitionService, 
    IServiceProvider serviceProvider, 
    IWorkflowInstanceVariableReader variableReader,
    IWorkflowInstanceVariableWriter variableWriter) : IWorkflowInstanceVariableManager
{
    public async Task<IEnumerable<ResolvedVariable>> GetVariables(string workflowInstanceId, CancellationToken cancellationToken = default)
    {
        var workflowExecutionContext = await GetWorkflowExecutionContextAsync(workflowInstanceId, cancellationToken);
        if (workflowExecutionContext == null) return [];
        return await variableReader.GetVariables(workflowExecutionContext, cancellationToken);
    }

    public Task<IEnumerable<ResolvedVariable>> GetVariables(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default)
    {
        return variableReader.GetVariables(workflowExecutionContext, cancellationToken);
    }

    public async Task<IEnumerable<ResolvedVariable>> SetVariables(string workflowInstanceId, IEnumerable<VariableUpdateValue> variables, CancellationToken cancellationToken = default)
    {
        var workflowExecutionContext = await GetWorkflowExecutionContextAsync(workflowInstanceId, cancellationToken);
        if (workflowExecutionContext == null) return [];
        var resolvedVariables = await variableWriter.SetVariables(workflowExecutionContext, variables, cancellationToken);
        await workflowInstanceManager.SaveAsync(workflowExecutionContext, cancellationToken);
        return resolvedVariables;
    }

    public Task<IEnumerable<ResolvedVariable>> SetVariables(WorkflowExecutionContext workflowExecutionContext, IEnumerable<VariableUpdateValue> variables, CancellationToken cancellationToken = default)
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