using System;
using System.Linq;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.UserTask.Activities
{
    /// <summary>
    /// Stores a set of possible user actions and halts the workflow until one of the actions has been performed.
    /// </summary>
    [ActivityDefinition(
        Category = "User Tasks",
        Description = "Triggers when a user action is received."
    )]
    public class UserTask : Activity
    {
        [ActivityProperty(
            Type = ActivityPropertyTypes.List,
            Hint = "Enter a comma-separated list of available actions"
        )]
        public string[] Actions
        {
            get => GetState(() => new string[0]);
            set => SetState(value);
        }

        protected override bool OnCanExecute(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext context)
        {
            var userAction = GetUserAction(context);

            return Actions.Contains(userAction, StringComparer.OrdinalIgnoreCase);
        }

        protected override IActivityExecutionResult OnExecute(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext context)
        {
            return Suspend();
        }

        protected override IActivityExecutionResult OnResume(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext context)
        {
            var userAction = GetUserAction(context);
            return Done(userAction);
        }

        private string GetUserAction(ActivityExecutionContext context) => context.Input?.GetValue<string>();
    }
}