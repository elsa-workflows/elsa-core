using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
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
        public ICollection Collection { get; set; } = new Collection<object>();

        private IList<object>? CollectionCopy { get; set; }

        private int? CurrentIndex { get; set; }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(
            ActivityExecutionContext context,
            CancellationToken cancellationToken)
        {
            var collection = CollectionCopy;

            if (collection == null)
                CollectionCopy = collection = Collection.Cast<object>().ToList();

            var currentIndex = CurrentIndex ?? 0;

            if (currentIndex < collection.Count)
            {
                var input = collection[currentIndex];
                CurrentIndex = currentIndex + 1;
                return Combine(Schedule(this), Done(OutcomeNames.Iterate, input));
            }

            CurrentIndex = null;
            CollectionCopy = null;
            return Done();
        }
    }
}