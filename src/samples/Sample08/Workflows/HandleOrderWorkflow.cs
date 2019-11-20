using System;
using Elsa.Activities;
using Elsa.Activities.MassTransit.Activities;
using Elsa.Activities.Timers.Activities;
using Elsa.Expressions;
using Elsa.Scripting.JavaScript;
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
                .Then<TimerEvent>(activity => activity.TimeoutExpression = new LiteralExpression<TimeSpan>("00:00:05"))
                .Then<PublishMassTransitMessage>(activity =>
                    {
                        activity.Message = new JavaScriptExpression<OrderShipped>("return { correlationId: correlationId(), order: order}");
                        activity.MessageType = typeof(OrderShipped);
                    }
                );
        }
    }
}