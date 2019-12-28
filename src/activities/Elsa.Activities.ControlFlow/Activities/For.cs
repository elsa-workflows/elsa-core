using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.ControlFlow.Activities
{
    [ActivityDefinition(Category = "Control Flow", Description = "Iterate between two numbers.", Icon = "far fa-circle")]
    public class For : Activity
    {
        [ActivityProperty(Hint = "An expression that evaluates to the starting number.")]
        public IWorkflowExpression<int> Start
        {
            get => GetState<IWorkflowExpression<int>>();
            set => SetState(value);
        }

        [ActivityProperty(Hint = "An expression that evaluates to the ending number.")]
        public IWorkflowExpression<int> End
        {
            get => GetState<IWorkflowExpression<int>>();
            set => SetState(value);
        }

        [ActivityProperty(Hint = "An expression that evaluates to the incrementing number on each step.")]
        public IWorkflowExpression<int> Step
        {
            get => GetState<IWorkflowExpression<int>>(() => new CodeExpression<int>(() => 1));
            set => SetState(value);
        }

        public IActivity Activity { get; set; }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var startValue = await context.EvaluateAsync(Start, cancellationToken);
            var endValue = await context.EvaluateAsync(End, cancellationToken);
            var step = await context.EvaluateAsync(Step, cancellationToken);
            var results = new List<IActivityExecutionResult> { Done() };

            for (var i = endValue; i > startValue; i -= step)
            {
                var result = ScheduleActivity(Activity, Variable.From(i));
                results.Add(result);
            }

            return Combine(results);
        }
    }
}