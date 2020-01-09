using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Models;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using ScheduledActivity = Elsa.Services.Models.ScheduledActivity;

namespace Elsa.Activities.ControlFlow
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
        
        private int? CurrentValue
        {
            get => GetState<int?>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            var startValue = await workflowExecutionContext.EvaluateAsync(Start, activityExecutionContext, cancellationToken);
            var endValue = await workflowExecutionContext.EvaluateAsync(End, activityExecutionContext, cancellationToken);
            var step = await workflowExecutionContext.EvaluateAsync(Step, activityExecutionContext, cancellationToken);
            var currentValue = CurrentValue ?? startValue;

            if (currentValue < endValue)
            {
                var input = currentValue;
                currentValue += step;
                CurrentValue = currentValue;
                return Combine(Schedule(this), Done(OutcomeNames.Iterate, Variable.From(input)));
            }

            CurrentValue = null;
            return Done();
        }
    }
}