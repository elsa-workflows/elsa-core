using System.Collections.Generic;
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
            SupportedSyntaxes = new[] { SyntaxNames.Json }
        )]
        public ISet<string> Branches { get; set; } = new HashSet<string>();

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => Outcomes(Branches);
    }
}