using System.Collections.Generic;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    [Activity(Category = "Control Flow", Description = "Fork workflow execution into multiple branches.")]
    public class Fork : Activity
    {
        [ActivityProperty(
            UIHint = ActivityPropertyUIHints.MultiText,
            Hint = "Enter one or more branch names.")]
        public ISet<string> Branches { get; set; } = new HashSet<string>();

        protected override IActivityExecutionResult OnExecute() => Combine(Done(), Outcomes(Branches));
    }
}