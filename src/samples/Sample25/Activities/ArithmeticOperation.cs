using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa;
using Elsa.Activities.ControlFlow.Activities;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services.Models;

namespace Sample25.Activities
{
    public abstract class ArithmeticOperation : Join
    {
        public ArithmeticOperation()
        {
            Mode = JoinMode.WaitAll;
        }

        public WorkflowExpression<double[]> Values
        {
            get => GetState<WorkflowExpression<double[]>>();
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            var activityExecutionResult = await base.OnExecuteAsync(context, cancellationToken);

            if (IsCompleted(activityExecutionResult))
            {
                var values = await context.EvaluateAsync(Values, cancellationToken);
                var result = Calculate(values);

                Output.SetVariable("Result", result);
            }

            return activityExecutionResult;
        }

        protected abstract double Calculate(params double[] values);

        private bool IsCompleted(IActivityExecutionResult result) => result is OutcomeResult outcome && outcome.EndpointNames.Any(x => x == OutcomeNames.Done);
    }
}