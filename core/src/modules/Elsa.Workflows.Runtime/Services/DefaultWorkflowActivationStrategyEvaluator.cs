namespace Elsa.Workflows.Runtime;

public class DefaultWorkflowActivationStrategyEvaluator(IEnumerable<IWorkflowActivationStrategy> strategies) : IWorkflowActivationStrategyEvaluator
{
    public async Task<bool> CanStartWorkflowAsync(WorkflowActivationStrategyEvaluationContext context)
    {
        var workflow = context.Workflow;
        var strategyType = workflow.Options.ActivationStrategyType;

        if (strategyType == null)
            return true;
        
        var strategy = strategies.FirstOrDefault(x => x.GetType() == strategyType);

        if (strategy == null)
            throw new InvalidOperationException($"Workflow activation strategy '{strategyType.FullName}' is configured but not registered. Register an IWorkflowActivationStrategy implementation for this type or select a registered strategy.");
        
        var correlationId = context.CorrelationId;
        var cancellationToken = context.CancellationToken;
        var strategyContext = new WorkflowInstantiationStrategyContext(workflow, correlationId, cancellationToken);
        return await strategy.GetAllowActivationAsync(strategyContext);
    }
}
