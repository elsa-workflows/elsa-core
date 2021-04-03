using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Entity
{
    [Trigger(
        Category = "Entity",
        DisplayName = "Entity Changed",
        Description = "Triggers when an entity was added, updated or deleted.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class EntityChanged : Activity
    {
        [ActivityProperty(UIHint = ActivityPropertyUIHints.SingleLine, Hint = "The Entity Name to observe. Matches any entity if no value is specified.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? EntityName { get; set; }

        [ActivityProperty(
            UIHint = ActivityPropertyUIHints.Dropdown,
            Hint = "The Entity Changed Action to observe. Matches any action if no value is specified.",
            Options = new[] { "Added", "Updated", "Deleted" },
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public EntityChangedAction? Action { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => context.WorkflowExecutionContext.IsFirstPass ? Done(context.Input) : Suspend();
        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context) => Done(context.Input);
    }
}