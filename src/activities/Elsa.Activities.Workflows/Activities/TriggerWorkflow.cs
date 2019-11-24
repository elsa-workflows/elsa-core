using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Models;
using Elsa.Scripting.JavaScript;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Workflows.Activities
{
    [ActivityDefinition(
        Category = "Workflows",
        Description = "Trigger all workflows that start with or are blocked on the specified activity type.",
        Icon = "fas fa-sitemap"
    )]
    public class TriggerWorkflow : Activity
    {
        private readonly IWorkflowInvoker workflowInvoker;

        public TriggerWorkflow(IWorkflowInvoker workflowInvoker)
        {
            this.workflowInvoker = workflowInvoker;
        }

        [ActivityProperty(Hint = "An expression that evaluates to the activity type to use when triggering workflows.")]
        public WorkflowExpression<string> ActivityType
        {
            get => GetState(() => new LiteralExpression(""));
            set => SetState(value);
        }

        [ActivityProperty(
            Hint = "An expression that evaluates to a dictionary to be provided as input when triggering workflows."
        )]
        public WorkflowExpression<Variables> Input
        {
            get => GetState(() => new JavaScriptExpression<Variables>("{}"));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "An expression that evaluates to the correlation ID to use when triggering workflows.")]
        public WorkflowExpression<string> CorrelationId
        {
            get => GetState(() => new LiteralExpression(Guid.NewGuid().ToString()));
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            var activityType = await context.EvaluateAsync(ActivityType, cancellationToken);
            var input = await context.EvaluateAsync(Input, cancellationToken);
            var correlationId = await context.EvaluateAsync(CorrelationId, cancellationToken);

            await workflowInvoker.TriggerAsync(
                activityType,
                input,
                correlationId,
                cancellationToken: cancellationToken
            );

            return Done();
        }
    }
}