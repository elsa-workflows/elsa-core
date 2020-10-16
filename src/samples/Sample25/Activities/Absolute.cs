using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Sample25.Activities
{
    public class Absolute : Activity
    {
        public WorkflowExpression<double> ValueExpression
        {
            get => GetState<WorkflowExpression<double>>();
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            var value = await context.EvaluateAsync(ValueExpression, cancellationToken);
            var result = Math.Abs(value);

            Output.SetVariable("Result", result);
            return Done();
        }
    }
}