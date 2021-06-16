using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    [Activity(
        Category = "Control Flow",
        Description = "Iterate over a collection in parallel.",
        Outcomes = new[] { OutcomeNames.Iterate, OutcomeNames.Done }
    )]
    public class ParallelForEach : Activity
    {
        [ActivityInput(
            Hint = "A collection of items to iterate over.",
            UIHint = ActivityInputUIHints.MultiLine,
            DefaultSyntax = SyntaxNames.Json,
            SupportedSyntaxes = new[] { SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public ICollection<object> Items { get; set; } = new Collection<object>();
        
        

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            var items = Items.Reverse().ToList();
            var results = new List<IActivityExecutionResult> { Done() };
            results.AddRange(items.Select(x => Outcome(OutcomeNames.Iterate, x)));
            return Combine(results);
        }
    }
}