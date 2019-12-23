using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

namespace Sample04.Activities
{
    public abstract class ArithmeticOperation : Activity
    {
        public IWorkflowExpression<double[]> Values
        {
            get => GetState<IWorkflowExpression<double[]>>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var values = await context.EvaluateAsync(Values, cancellationToken);
            var sum = Calculate(values);

            return Done(sum);
        }

        protected abstract double Calculate(params double[] values);
    }
}