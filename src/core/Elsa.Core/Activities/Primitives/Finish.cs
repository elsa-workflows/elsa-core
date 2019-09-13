using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using NodaTime;

namespace Elsa.Activities.Primitives
{
    public class Finish : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;
        private readonly IClock clock;

        public Finish(IWorkflowExpressionEvaluator expressionEvaluator, IClock clock)
        {
            this.expressionEvaluator = expressionEvaluator;
            this.clock = clock;
        }

        public WorkflowExpression<Variables> WorkflowOutput
        {
            get => GetState(() => new JavaScriptExpression<Variables>("{}"));
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(
            WorkflowExecutionContext workflowContext,
            CancellationToken cancellationToken)
        {
            var workflowOutput = await expressionEvaluator.EvaluateAsync(
                WorkflowOutput,
                workflowContext,
                cancellationToken
            );
            
            workflowContext.Workflow.Output = workflowOutput;

            return Finish(clock.GetCurrentInstant());
        }
    }
}