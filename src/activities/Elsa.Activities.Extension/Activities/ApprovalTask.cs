using System;
using System.Linq;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using System.Collections.Generic;
using Elsa.Expressions;
using Elsa.Scripting.JavaScript;

namespace Elsa.Activities.UserTask.Activities
{
    /// <summary>
    /// 存储一组可能的用户操作，并暂停工作流，直到其中一个操作已经执行。
    /// </summary>
    [ActivityDefinition(
        Category = "用户任务",
        Description = "审批活动，当接收到用户操作时触发。",
        DisplayName = "审批人",
        Outcomes = "x => x.state.actions"
    )]
    public class ApprovalTask : Activity
    {
        [ActivityProperty(
            Type = ActivityPropertyTypes.List,
            Hint = "输入可能的操作，用逗号分隔。例：Accept,Reject （注意不要使用中文）",
            Label = "动作"
        )]
        public IList<string> Actions
        {
            get => GetState(() => new List<string> { "Accept", "Reject", "TurnToOhter" });
            set => SetState(value);
        }

        [ActivityProperty(
            Type = ActivityPropertyTypes.Select,
            Hint = "审批人设置",
            Label = "审批人选择方式"
        )]
        [SelectOptions("指定成员", "申请人自选")]
        public ChioceMode Choice
        {
            get => GetState(() => ChioceMode.Specify);
            set => SetState(value);
        }

        [ActivityProperty(Label = "组织机构范围", Hint = "组织机构范围")]
        public WorkflowExpression<string> OrganizationScope
        {
            get => GetState(() => new JavaScriptExpression<string>(""));
            set => SetState(value);
        }
        [ActivityProperty(
            Label = "参与者",
            Hint = "当节活动的审批人"
        )]
        public WorkflowExpression<string> Actors
        {
            get => GetState<WorkflowExpression<string>>();
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
            (string)context.Workflow.Input.GetVariable("UserAction");
    }
}