using System.Collections.Generic;
using System.Linq;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    [ActivityAttribute(
        DisplayName = "If/Else",
        Category = "Control Flow",
        Description = "Evaluate multiple conditions and continue execution depending on the results.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class IfThen : Activity
    {
        [ActivityProperty(Hint = "The conditions to evaluate.")]
        public ICollection<IfThenCondition> Conditions { get; set; } = new List<IfThenCondition>();

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            var outcomes = new[] { OutcomeNames.Done }.Concat(Conditions.Where(x => x.Condition).Select(x => x.Name));
            return Outcomes(outcomes);
        }
    }
}