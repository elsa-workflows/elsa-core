using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Results;
using Elsa.Scripting.JavaScript.Services;
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
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public TriggerWorkflow(IWorkflowInvoker workflowInvoker, IWorkflowExpressionEvaluator expressionEvaluator)
        {
            this.workflowInvoker = workflowInvoker;
            this.expressionEvaluator = expressionEvaluator;
        }

        [ActivityProperty(Hint = "An expression that evaluates to the activity type to use when triggering workflows.")]
        public WorkflowExpression<string> ActivityType
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(
            Hint = "An expression that evaluates to a dictionary to be provided as input when triggering workflows."
        )]
        public WorkflowExpression<Variables> Input
        {
            get => GetState(() => new WorkflowExpression<Variables>(JavaScriptExpressionEvaluator.SyntaxName, "{}"));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "An expression that evaluates to the correlation ID to use when triggering workflows.")]
        public WorkflowExpression<string> CorrelationId
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, Guid.NewGuid().ToString()));
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context,
            CancellationToken cancellationToken)
        {
            var activityType = await expressionEvaluator.EvaluateAsync(ActivityType, context, cancellationToken);
            var input = await expressionEvaluator.EvaluateAsync(Input, context, cancellationToken);
            var correlationId = await expressionEvaluator.EvaluateAsync(CorrelationId, context, cancellationToken);

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