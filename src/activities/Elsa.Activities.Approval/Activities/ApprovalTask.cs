using System;
using System.Linq;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.ApprovalTask.Activities
{
    /// <summary>
    /// Stores a set of possible user actions and halts the workflow until one of the actions has been performed.
    /// </summary>
    [ActivityDefinition(
        Category = "用户任务",
        Description = "审批活动，当接收到用户操作时触发。",
        Outcomes = "x => x.state.actions"
    )]
    public class ApprovalTask : Activity
    {
        [ActivityProperty(
            Type = ActivityPropertyTypes.List,
            Hint = "输入可用操作的逗号分隔列表"
        )]
        public IList[] Actions
        {
            get => GetState(() => new List<string>());
            set => SetState(value);
        }

        protected override bool OnCanExecute(WorkflowExecutionContext context)
        {
            var userAction = GetUserAction(context);

            return Actions.Contains(userAction, StringComparer.OrdinalIgnoreCase);
        }

        protected override ActivityExecutionResult OnExecute(WorkflowExecutionContext context)
        {
            return Halt(true);
        }

        protected override ActivityExecutionResult OnResume(WorkflowExecutionContext context)
        {
            var userAction = GetUserAction(context);
            return Outcome(userAction);
        }

        private string GetUserAction(WorkflowExecutionContext context) =>
            (string) context.Workflow.Input.GetVariable("ApprovalAction");
    }
}