using System.Collections.Generic;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    [ActivityDefinition(
        Category = "Control Flow",
        Description = "Fork workflow execution into multiple branches.",
        Icon = "fas fa-code-branch fa-rotate-180",
        Outcomes = new[] { "x => x.state.branches" })]
    public class Fork : Activity
    {
        [ActivityProperty(
            Hint = "Enter one or more names representing branches, separated with a comma. Example: Branch 1, Branch 2"
        )]
        public HashSet<string> Branches { get; set; } = new HashSet<string>();

        protected override IActivityExecutionResult OnExecute() => Done(Branches);
    }
}