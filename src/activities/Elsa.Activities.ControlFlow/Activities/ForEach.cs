using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Results;
using Elsa.Scripting.JavaScript;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.ControlFlow.Activities
{
    [ActivityDefinition(Category = "Control Flow", Description = "Iterate over a collection.", Icon = "far fa-circle")]
    public class ForEach : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public ForEach(IWorkflowExpressionEvaluator expressionEvaluator)
        {
            this.expressionEvaluator = expressionEvaluator;
        }

        [ActivityProperty(Hint = "Enter an expression that evaluates to an array of items to iterate over.")]
        public WorkflowExpression<IList<object>> CollectionExpression
        {
            get => GetState(() => new JavaScriptExpression<IList<object>>("[]"));
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

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(
            WorkflowExecutionContext context,
            CancellationToken cancellationToken)
        {
            var collection = await expressionEvaluator.EvaluateAsync(CollectionExpression, context, cancellationToken);
            var index = CurrentIndex;

            if (index >= collection.Count)
            {
                context.EndScope();
                CurrentIndex = 0;
                return Done();
            }

            var value = collection[index];
            CurrentIndex++;

            if (index == 0)
            {
                context.BeginScope();
            }

            context.CurrentScope.SetVariable(IteratorName, value);

            return Outcome(OutcomeNames.Iterate);
        }
    }
}