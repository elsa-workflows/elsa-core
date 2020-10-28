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
        Description = "抄送工作流执行情况给指定用户",
        DisplayName = "抄送"
    )]
    public class Notify : Activity
    {
        [ActivityProperty(
            Type = ActivityPropertyTypes.Select,
            Hint = "接收人设置",
            Label = "接收人选择方式"
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
            Label = "接收者",
            Hint = "当前活动的接收者，可以选择人员及角色，角色被限定在组织机构范围内。"
        )]
        public WorkflowExpression<string> Actors
        {
            get => GetState<WorkflowExpression<string>>();
            set => SetState(value);
        }

        protected override ActivityExecutionResult OnExecute(WorkflowExecutionContext context)
        {
            return Done();
        }



    }
}