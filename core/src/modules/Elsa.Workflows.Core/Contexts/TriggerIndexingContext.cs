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

    /// <summary>
    /// Set by the trigger being indexed to declare that, as configured, it deliberately registers no triggers at all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A trigger that returns no payloads is otherwise stored as a single placeholder row with a <c>null</c> payload, which the runtime's workflow validation
    /// reports as a trigger without a payload. For almost every trigger that is the right outcome: no payloads means its configuration is incomplete.
    /// A trigger whose decision to register anything depends on its own content, and that has nothing to register as configured, sets this instead,
    /// and the indexer then stores no row for it.
    /// </para>
    /// <para>
    /// It only takes effect when the trigger returns no payloads and completes without throwing. Payloads the trigger does return are indexed as usual,
    /// and a trigger that throws still gets the placeholder row, so a failure is never mistaken for a deliberate decision.
    /// </para>
    /// </remarks>
    public bool RegistersNoTriggers { get; set; }

    public T? Get<T>(Input<T>? input) => ExpressionExecutionContext.Get(input);
}