using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Models;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    [ActivityDefinition(
        Category = "Control Flow",
        Description = "Iterate over a collection.",
        Icon = "far fa-circle",
        Outcomes = new[] {OutcomeNames.Iterate, OutcomeNames.Done}
    )]
    public class ForEach : Activity
    {
        [ActivityProperty(Hint = "Enter an expression that evaluates to a collection of items to iterate over.")]
        public IWorkflowExpression<ICollection> Collection
        {
            get => GetState<IWorkflowExpression<ICollection>>();
            set => SetState(value);
        }

        private IList<object>? CollectionCopy
        {
            get => GetState<IList<object>?>();
            set => SetState(value);
        }

        private int? CurrentIndex
        {
            get => GetState<int?>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(
            ActivityExecutionContext context,
            CancellationToken cancellationToken)
        {
            var collection = CollectionCopy;

            if (collection == null)
                CollectionCopy = collection =
                    ((await context.EvaluateAsync(Collection, cancellationToken)) ?? new object[0]).Cast<object>()
                    .ToList();

            var currentIndex = CurrentIndex ?? 0;

            if (currentIndex < collection.Count)
            {
                var input = collection[currentIndex];
                CurrentIndex = currentIndex + 1;
                return Combine(Schedule(this), Done(OutcomeNames.Iterate, Variable.From(input)));
            }

            CurrentIndex = null;
            CollectionCopy = null;
            return Done();
        }
    }
}