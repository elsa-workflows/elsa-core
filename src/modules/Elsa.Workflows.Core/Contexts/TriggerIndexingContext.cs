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

    /// <summary>
    /// The default stimulus name applied to the payloads returned by the trigger being indexed. Defaults to the activity type name.
    /// </summary>
    /// <remarks>
    /// This is a single value shared by every payload of the trigger: when it is assigned more than once, the last write applies to all of them.
    /// A trigger that needs to register payloads under more than one stimulus name should return <see cref="NamedTriggerPayload"/> instances instead,
    /// which carry a name per payload and take precedence over this property.
    /// </remarks>
    public string TriggerName { get; set; } = trigger.Type;

    public T? Get<T>(Input<T>? input) => ExpressionExecutionContext.Get(input);
}