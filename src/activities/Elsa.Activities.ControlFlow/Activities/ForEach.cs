using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.ControlFlow.Activities
{
    [ActivityDefinition(Category = "Control Flow", Description = "Iterate over a collection.", Icon = "far fa-circle")]
    public class ForEach : Activity
    {
        [ActivityProperty(Hint = "Enter an expression that evaluates to an array of items to iterate over.")]
        public IWorkflowExpression<IList<object>> Collection
        {
            get => GetState<IWorkflowExpression<IList<object>>>();
            set => SetState(value);
        }

        [ActivityProperty(Hint = "Enter a name for the iterator variable.")]
        public string IteratorName
        {
            get => GetState(() => "CurrentValue");
            set => SetState(value);
        }

        public int CurrentIndex
        {
            get => GetState(() => 0);
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var collection = await context.EvaluateAsync(Collection, cancellationToken) ?? new object[0];
            var index = CurrentIndex;

            if (index >= collection.Count)
            {
                context.WorkflowExecutionContext.EndScope();
                CurrentIndex = 0;
                return Done();
            }

            var value = collection[index];
            CurrentIndex++;

            if (index == 0)
            {
                context.WorkflowExecutionContext.BeginScope();
            }

            context.WorkflowExecutionContext.CurrentScope.SetVariable(IteratorName, value);

            return Outcome(OutcomeNames.Iterate);
        }
    }
}