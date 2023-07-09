using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core;

public class TriggerIndexingContext
{
    public WorkflowIndexingContext WorkflowIndexingContext { get; }
    public ExpressionExecutionContext ExpressionExecutionContext { get; }
    public ITrigger Trigger { get; }
    public CancellationToken CancellationToken { get; }

    public TriggerIndexingContext(WorkflowIndexingContext workflowIndexingContext, ExpressionExecutionContext expressionExecutionContext, ITrigger trigger, CancellationToken cancellationToken)
    {
        WorkflowIndexingContext = workflowIndexingContext;
        ExpressionExecutionContext = expressionExecutionContext;
        Trigger = trigger;
        CancellationToken = cancellationToken;
    }

    public T? Get<T>(Input<T>? input) => ExpressionExecutionContext.Get(input);
}