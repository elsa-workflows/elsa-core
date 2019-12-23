﻿using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Models;
using Elsa.Scripting;
using Elsa.Scripting.JavaScript;
using Elsa.Scripting.JavaScript.Services;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Workflows.Activities
{
    /// <summary>
    /// Triggers the specified signal.
    /// </summary>
    [ActivityDefinition(
        Category = "Workflows",
        Description = "Trigger the specified signal.",
        Icon = "fas fa-broadcast-tower"
    )]
    public class TriggerSignal : Activity
    {
        private readonly IWorkflowRunner workflowRunner;

        public TriggerSignal(IWorkflowRunner workflowRunner)
        {
            this.workflowRunner = workflowRunner;
        }

        [ActivityProperty(Hint = "An expression that evaluates to the name of the signal to trigger.")]
        public IWorkflowExpression<string> Signal
        {
            get => GetState<IWorkflowExpression<string>>();
            set => SetState(value);
        }

        [ActivityProperty(
            Hint = "An expression that evaluates to a dictionary to be provided as input when signaling."
        )]
        public IWorkflowExpression<Variables> Input
        {
            get => GetState<IWorkflowExpression<Variables>>();
            set => SetState(value);
        }

        [ActivityProperty(Hint = "An expression that evaluates to the correlation ID to use when signaling.")]
        public IWorkflowExpression<string> CorrelationId
        {
            get => GetState<IWorkflowExpression<string>>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            var signal = await context.EvaluateAsync(Signal, cancellationToken);
            var input = (await context.EvaluateAsync(Input, cancellationToken)) ?? new Variables();
            var correlationId = await context.EvaluateAsync(CorrelationId, cancellationToken);

            input.SetVariable("Signal", signal);

            await workflowRunner.TriggerAsync(
                nameof(Signaled),
                input,
                correlationId,
                cancellationToken: cancellationToken
            );

            return Done();
        }
    }
}