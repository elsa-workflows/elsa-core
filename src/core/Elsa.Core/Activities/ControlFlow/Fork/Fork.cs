using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Events;
using Elsa.Expressions;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using MediatR;
using Newtonsoft.Json.Linq;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    [Activity(Category = "Control Flow", Description = "Fork workflow execution into multiple branches.")]
    public class Fork : Activity
    {
        [ActivityProperty(
            Hint = "Enter one or more branch names.",
            UIHint = ActivityPropertyUIHints.MultiText,
            DefaultSyntax = SyntaxNames.Json,
            SupportedSyntaxes = new[] { SyntaxNames.Json }
        )]
        public ISet<string> Branches { get; set; } = new HashSet<string>();

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => Outcomes(Branches);
    }
}