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
using System.Threading.Tasks;
using System.Threading;

namespace Elsa.Activities.UserTask.Activities
{
    /// <summary>
    /// 存储一组可能的用户操作，并暂停工作流，直到其中一个操作已经执行。
    /// </summary>
    [ActivityDefinition(
        Category = "用户任务",
        Description = "工作流程的发起者",
        DisplayName = "申请人"
    )]
    public class Applicant : Activity
    {

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

        [ActivityProperty(Label = "组织机构范围", Hint = "申请人自选审批人时被限定的组织机构")]
        public WorkflowExpression<string> Organization
        {
            get => GetState(() => new JavaScriptExpression<string>(""));
            set => SetState(value);
        }
        [ActivityProperty(
            Label = "审批人范围",
            Hint = "申请人自选审批人的范围，可以混合用户和角色，如果为空，则可选组织机构范围内的所有人和角色"
        )]
        public WorkflowExpression<string> Approver
        {
            get => GetState<WorkflowExpression<string>>();
            set => SetState(value);
        }
        //Recipienter
        [ActivityProperty(
            Label = "抄送人范围",
            Hint = "申请人自选抄送人的范围，可以混合用户和角色，如果为空，则可选组织机构范围内的所有人和角色"
        )]
        public WorkflowExpression<string> Receiver
        {
            get => GetState<WorkflowExpression<string>>();
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context,CancellationToken cancellationToken)
        {
            {
                Actors actors = new Actors
                {
                    Approver = await context.EvaluateAsync(Approver,cancellationToken),
                    Receiver = await context.EvaluateAsync(Receiver,cancellationToken)
                };
                //下一节点，取由申请人自选的审批人
                Output.SetVariable("Actors", actors);
                return Done();
            }

        }
    }
}