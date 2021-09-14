using System.Collections.Generic;
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
    [Activity(Category = "Control Flow", Description = "Fork workflow execution into multiple branches.")]
    public class Fork : Activity
    {
        [ActivityInput(
            Hint = "Enter one or more branch names.",
            UIHint = ActivityInputUIHints.MultiText,
            DefaultSyntax = SyntaxNames.Json,
            SupportedSyntaxes = new[] { SyntaxNames.Json },
            IsDesignerCritical = true
        )]
        public ISet<string> Branches { get; set; } = new HashSet<string>();

        // Schedule the branches in reverse order so that the first branch will execute before the second one, etc.
        // This is important for scenarios where the user needs to schedule a blocking activity like a signal received event before actually sending a signal from a second second branch, causing a response signal to be triggered from another workflow (as an example).
        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => Outcomes(Branches.Reverse());
    }
}