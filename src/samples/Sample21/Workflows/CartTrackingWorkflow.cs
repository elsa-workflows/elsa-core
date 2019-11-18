using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Elsa.Activities;
using Elsa.Activities.ControlFlow.Activities;
using Elsa.Activities.Email.Activities;
using Elsa.Activities.Http.Activities;
using Elsa.Activities.MassTransit.Activities;
using Elsa.Activities.Workflows.Activities;
using Elsa.Expressions;
using Elsa.Scripting.JavaScript;
using Elsa.Services;
using Elsa.Services.Models;
using Sample21.Messages;

namespace Sample21.Workflows
{
    public class CartTrackingWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder.StartWith<ReceiveMassTransitMessage>(activity => activity.MessageType = typeof(CartItemAdded))
                .Then<SetVariable>(activity =>
                {
                    activity.VariableName = "LastUpdateTimestamp";
                    activity.ValueExpression = new JavaScriptExpression<DateTime>("lastResult().Timestamp");
                })
                .Then<SetVariable>(activity =>
                {
                    activity.VariableName = "IsExpired";
                    activity.ValueExpression = new JavaScriptExpression<bool>("false");
                })
                .Then<Fork>(
                    action => action.Branches = new [] {"Item-Added", "Cart-Expired"},
                    fork =>
                    {
                        fork.When("Item-Added")
                            .Then<ScheduleMassTransitMessage>(
                                activity =>
                                {
                                    activity.MessageType = typeof(CartExpiredEvent);
                                    activity.EndpointAddress = new Uri("rabbitmq://localhost/shopping_cart_state");
                                    activity.Message = new JavaScriptExpression<CartExpiredEvent>("return { correlationId: correlationId(), cardId: correlationId() }");
                                }).WithName("ScheduleExpire")
                            .Then<ReceiveMassTransitMessage>(activity => activity.MessageType = typeof(CartItemAdded))
                            .Then("ScheduleExpire");

                        fork.When("Cart-Expired")
                            .Then<ReceiveMassTransitMessage>(activity => activity.MessageType = typeof(CartExpiredEvent))
                            .Then<SetVariable>(activity =>
                            {
                                activity.VariableName = "IsExpired";
                                activity.ValueExpression = new JavaScriptExpression<bool>("true");
                            })
                            .Then<SendMassTransitMessage>(activity =>
                            {
                                activity.Message = new JavaScriptExpression<CartRemovedEvent>("return { cardId: correlationId() };");
                                activity.MessageType = typeof(CartRemovedEvent);
                            })
                            .Then("Join");
                    })
                .Then<Join>(x => x.Mode = Join.JoinMode.WaitAny).WithName("Join");

        }
    }
}