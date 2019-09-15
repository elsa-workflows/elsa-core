using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Workflows
{
    public class TriggerWorkflow : Activity
    {
        private readonly IWorkflowInvoker workflowInvoker;
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public TriggerWorkflow(IWorkflowInvoker workflowInvoker, IWorkflowExpressionEvaluator expressionEvaluator)
        {
            this.workflowInvoker = workflowInvoker;
            this.expressionEvaluator = expressionEvaluator;
        }

        public WorkflowExpression<string> ActivityType
        {
            get => GetState(() => new Literal(""));
            set => SetState(value);
        }

        public WorkflowExpression<Variables> Input
        {
            get => GetState(() => new JavaScriptExpression<Variables>("{}"));
            set => SetState(value);
        }

        public WorkflowExpression<string> CorrelationId
        {
            get => GetState(() => new Literal(Guid.NewGuid().ToString()));
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