using Elsa.Contracts;

namespace Elsa.Models;

public class TriggerIndexingContext
{
    public WorkflowIndexingContext WorkflowIndexingContext { get; }
    public ExpressionExecutionContext ExpressionExecutionContext { get; }
    public ITrigger Trigger { get; }

    public TriggerIndexingContext(WorkflowIndexingContext workflowIndexingContext, ExpressionExecutionContext expressionExecutionContext, ITrigger trigger)
    {
        WorkflowIndexingContext = workflowIndexingContext;
        ExpressionExecutionContext = expressionExecutionContext;
        Trigger = trigger;
    }
}