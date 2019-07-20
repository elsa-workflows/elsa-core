using System;
using Elsa.Activities.MassTransit.Activities;
using Elsa.Activities.Timers.Activities;
using Elsa.Activities.Primitives;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Sample08.Messages;

namespace Sample08.Workflows
{
    public class HandleOrderWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .StartWith<ReceiveMassTransitMessage>(activity => activity.MessageType = typeof(CreateOrder))
                .Then<SetVariable>(
                    activity =>
                    {
                        activity.VariableName = "order";
                        activity.ValueExpression = new JavaScriptExpression<object>("lastResult().Order");
                    }
                )
                .Then<TimerEvent>(activity => activity.TimeoutExpression = new PlainTextExpression<TimeSpan>("00:00:05"))
                .Then<SendMassTransitMessage>(activity =>
                    {
                        activity.Message = new JavaScriptExpression<OrderShipped>("return {order: order}");
                        activity.MessageType = typeof(OrderShipped);
                    }
                );
        }
    }
}