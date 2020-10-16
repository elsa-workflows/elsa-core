using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Results;
using Elsa.Services.Models;

namespace Sample04.Activities
{
    public abstract class ArithmeticOperation : Activity
    {
        public WorkflowExpression<double[]> Values
        {
            get => GetState<WorkflowExpression<double[]>>();
            set => SetState(value);
        }

        public string ResultVariableName
        {
            get => GetState<string>();
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            var values = await context.EvaluateAsync(Values, cancellationToken);
            var sum = Calculate(values);

            context.SetLastResult(sum);
            context.CurrentScope.SetVariable(ResultVariableName, sum);
            
            return Done();
        }

        protected abstract double Calculate(params double[] values);
    }
}