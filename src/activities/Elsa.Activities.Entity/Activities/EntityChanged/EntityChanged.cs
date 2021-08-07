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
        [ActivityInput(UIHint = ActivityInputUIHints.SingleLine, Hint = "The Entity Name to observe. Matches any entity if no value is specified.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? EntityName { get; set; }

        [ActivityInput(
            UIHint = ActivityInputUIHints.Dropdown,
            Hint = "The Entity Changed Action to observe. Matches any action if no value is specified.",
            Options = new[] { "Added", "Updated", "Deleted" },
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public EntityChangedAction? Action { get; set; }

        [ActivityOutput] public object? Output { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => context.WorkflowExecutionContext.IsFirstPass ? ExecuteInternal(context) : Suspend();

        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context) => ExecuteInternal(context);

        private IActivityExecutionResult ExecuteInternal(ActivityExecutionContext context)
        {
            Output = context.Input;
            return Done();
        }
    }
}