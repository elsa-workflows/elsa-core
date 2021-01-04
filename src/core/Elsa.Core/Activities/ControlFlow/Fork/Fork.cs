using System.Collections.Generic;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    [Activity(
        Category = "Control Flow",
        Description = "Fork workflow execution into multiple branches.",
        Outcomes = new[] { "x => x.state.branches" })]
    public class Fork : Activity
    {
        [ActivityProperty(Hint = "Enter one or more names representing branches, separated with a comma. Example: Branch 1, Branch 2")]
        public HashSet<string> Branches { get; set; } = new();

        protected override IActivityExecutionResult OnExecute() => Combine(Done(), Outcomes(Branches));
    }
}