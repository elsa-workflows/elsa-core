namespace Elsa.Workflows.Runtime;

public interface IWorkflowActivationStrategyEvaluator
{
    public Task<bool> CanStartWorkflowAsync(WorkflowActivationStrategyEvaluationContext context);
}