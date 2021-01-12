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
        DisplayName = "Switch",
        Category = "Control Flow",
        Description = "Evaluate multiple conditions and continue execution depending on the results.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class Switch : Activity
    {
        [ActivityProperty(Hint = "The conditions to evaluate.")]
        public ICollection<SwitchCase> Cases { get; set; } = new List<SwitchCase>();

        [ActivityProperty(Hint = "The conditions to evaluate.")]
        public SwitchMode Mode { get; set; } = SwitchMode.MatchFirst;
        
        private bool Evaluated
        {
            get => GetState<bool>();
            set => SetState(value);
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            if (Evaluated)
            {
                Evaluated = false;
                return Done();
            }
            
            var matches = Cases.Where(x => x.Condition).Select(x => x.Name).ToList();
            var results = Mode == SwitchMode.MatchFirst ? matches.Any() ? new[] { matches.First() } : new string[0] : matches.ToArray();
            var outcomes = new[] { OutcomeNames.Done }.Concat(results);
            
            Evaluated = true;
            return Outcomes(outcomes);
        }
    }
}