using System;
using System.Net;
using System.Net.Http;
using Elsa.Activities.Email.Activities;
using Elsa.Activities.Http.Activities;
using Elsa.Activities.Timers.Activities;
using Elsa.Core.Activities.Primitives;
using Elsa.Core.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Sample08.Activities;
using Sample08.Messages;

namespace Sample08.Workflows
{
    public class HandleOrderWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .StartWith<ReceiveMassTransitMessage<CreateOrder>>()
                .Then<SetVariable>(
                    activity =>
                    {
                        activity.VariableName = "order";
                        activity.ValueExpression = new JavaScriptExpression<object>("lastResult()");
                    }
                )
                .Then<TimerEvent>(activity => activity.TimeoutExpression = new PlainTextExpression<TimeSpan>("00:00:10"))
                .Then<SendMassTransitMessage<CreateOrder>>(activity => activity.Message = new JavaScriptExpression<CreateOrder>("order"));
        }
    }
}