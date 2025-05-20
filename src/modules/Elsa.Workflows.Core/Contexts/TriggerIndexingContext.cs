using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Models;

namespace Elsa.Workflows;

public class TriggerIndexingContext(WorkflowIndexingContext workflowIndexingContext, ExpressionExecutionContext expressionExecutionContext, ITrigger trigger, CancellationToken cancellationToken)
{
    public WorkflowIndexingContext WorkflowIndexingContext { get; } = workflowIndexingContext;
    public ExpressionExecutionContext ExpressionExecutionContext { get; } = expressionExecutionContext;
    public ITrigger Trigger { get; } = trigger;
    public CancellationToken CancellationToken { get; } = cancellationToken;
    public string TriggerName { get; set; } = trigger.Type;

    public T? Get<T>(Input<T>? input) => ExpressionExecutionContext.Get(input);
}