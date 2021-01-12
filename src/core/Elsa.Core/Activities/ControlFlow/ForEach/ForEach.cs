using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    [Activity(
        Category = "Control Flow",
        Description = "Iterate over a collection.",
        Outcomes = new[] { OutcomeNames.Iterate, OutcomeNames.Done }
    )]
    public class ForEach : Activity
    {
        [ActivityProperty(Hint = "Enter an expression that evaluates to a collection of items to iterate over.")]
        public ICollection<object> Items { get; set; } = new Collection<object>();

        private IList<object>? ItemsCopy
        {
            get => GetState<IList<object>>();
            set => SetState(value);
        }

        private int? CurrentIndex
        {
            get => GetState<int?>();
            set => SetState(value);
        }
        
        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            var collection = ItemsCopy;

            if (collection == null)
                ItemsCopy = collection = Items.ToList();

            var currentIndex = CurrentIndex ?? 0;

            if (currentIndex < collection.Count)
            {
                var output = collection[currentIndex];
                CurrentIndex = currentIndex + 1;
                context.WorkflowInstance.Scopes.Push(Id);
                return Combine(Outcome(OutcomeNames.Iterate, output));
            }

            CurrentIndex = null;
            ItemsCopy = null;
            return Done();
        }
    }
}