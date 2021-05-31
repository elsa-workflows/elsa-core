using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Primitives
{
    [Activity(
        DisplayName = "Set Name",
        Description = "Set the name of the workflow instance.",
        Category = "Primitives",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class SetName : Activity
    {
        [ActivityInput(Hint = "The value to set as the workflow instance's name.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? Value { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            context.WorkflowExecutionContext.WorkflowInstance.Name = Value;
            return Done();
        }
    }
}