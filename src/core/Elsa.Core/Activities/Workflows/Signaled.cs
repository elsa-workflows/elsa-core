using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Workflows
{
    /// <summary>
    /// Halts workflow execution until the specified signal is received.
    /// </summary>
    public class Signaled : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public Signaled(IWorkflowExpressionEvaluator expressionEvaluator)
        {
            this.expressionEvaluator = expressionEvaluator;
        }

        public WorkflowExpression<string> Signal
        {
            get => GetState<WorkflowExpression<string>>();
            set => SetState(value);
        }

        protected override async Task<bool> OnCanExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            var signal = await expressionEvaluator.EvaluateAsync(Signal, context, cancellationToken);
            return context.Workflow.Input.HasVariable("Signal", signal);
        }

        protected override ActivityExecutionResult OnExecute(WorkflowExecutionContext context)
        {
            return Halt(true);
        }

        protected override ActivityExecutionResult OnResume(WorkflowExecutionContext context)
        {
            return Done();
        }
    }
}