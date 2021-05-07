using System;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;

namespace Elsa.Samples.CustomActivityOutcomes.Activities
{
    [Action(Description = "Tries to perform some work. Returns Done when successful, Failed otherwise.", Outcomes = new[] { OutcomeNames.Done, "Failed" })]
    public class TrySomething : Activity
    {
        private readonly Random _random;

        public TrySomething() => _random = new Random();

        protected override IActivityExecutionResult OnExecute()
        {
            // 50% chance of either returning Done or Failed.
            var value = _random.Next(0, 2);
            var outcome = value == 1 ? OutcomeNames.Done : "Failed";
            return Outcome(outcome);
        }
    }
}