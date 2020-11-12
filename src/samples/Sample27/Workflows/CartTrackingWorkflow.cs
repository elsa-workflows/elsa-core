using System;
using System.Resources;
using Elsa.Activities;
using Elsa.Activities.ControlFlow.Activities;
using Elsa.Activities.MassTransit.Activities;
using Elsa.Scripting.JavaScript;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Options;
using NodaTime;
using Sample27.Messages;

namespace Sample27.Workflows
{
    public class CartTrackingWorkflow : IWorkflow
    {
        private readonly AzureServiceBusSchedulerOptions options;

        public CartTrackingWorkflow(IOptions<AzureServiceBusSchedulerOptions> options)
        {
            this.options = options.Value;
        }

        public void Build(IWorkflowBuilder builder)
        {
            builder.StartWith<ReceiveMassTransitMessage>(activity => activity.MessageType = typeof(CartCreated))
                .Then<SetVariable>(activity =>
                {
                    activity.VariableName = "LastUpdateTimestamp";
                    activity.ValueExpression = new JavaScriptExpression<Instant>("instantFromDateTimeUtc(lastResult().Timestamp)");
                })
                .Then<SetVariable>(activity =>
                {
                    activity.VariableName = "IsExpired";
                    activity.ValueExpression = new JavaScriptExpression<bool>("false");
                })
                .Fork(
                    action => action.Branches = new [] {"Item-Added", "Cart-Expired", "Order-Submitted"},
                    fork =>
                    {
                        fork.When("Item-Added")
                            .Then<ScheduleSendMassTransitMessage>(
                                activity =>
                                {
                                    activity.MessageType = typeof(CartExpiredEvent);
                                    activity.EndpointAddress = new Uri($"{options.Host}/shopping_cart_state");
                                    activity.ScheduledTime = new JavaScriptExpression<DateTime>("plus(LastUpdateTimestamp, durationFromSeconds(10)).ToDateTimeUtc()");
                                    activity.Message = new JavaScriptExpression<CartExpiredEvent>("return { correlationId: correlationId(), cartId: correlationId() }");
                                }).WithName("ScheduleExpire")
                            .Then<SetVariable>(activity =>
                            {
                                activity.VariableName = "ScheduleTokenId";
                                activity.ValueExpression = new JavaScriptExpression<bool>("lastResult()");
                            })
                            .Then<ReceiveMassTransitMessage>(activity => activity.MessageType = typeof(CartItemAdded))
                            .Then<SetVariable>(activity =>
                            {
                                activity.VariableName = "LastUpdateTimestamp";
                                activity.ValueExpression = new JavaScriptExpression<Instant>("instantFromDateTimeUtc(lastResult().Timestamp)");
                            })
                            .Then<CancelScheduledMassTransitMessage>(activity => activity.TokenId = new JavaScriptExpression<Guid>("return ScheduleTokenId"))
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
                                activity.Message = new JavaScriptExpression<CartRemovedEvent>("return { cartId: correlationId() };");
                                activity.EndpointAddress = new Uri($"{options.Host}/shopping_cart_service");
                                activity.MessageType = typeof(CartRemovedEvent);
                            })
                            .Then("Join");

                        fork.When("OrderSubmitted")
                            .Then<ReceiveMassTransitMessage>(activity => activity.MessageType = typeof(OrderSubmitted))
                            .Then<SetVariable>(activity =>
                            {
                                activity.VariableName = "LastUpdateTimestamp";
                                activity.ValueExpression = new JavaScriptExpression<DateTime>("lastResult().Timestamp");
                            })
                            .Then<CancelScheduledMassTransitMessage>(activity => activity.TokenId = new JavaScriptExpression<Guid>("return ScheduleTokenId"))
                            .Then("Join");
                    })
                .Join(x => x.Mode = Join.JoinMode.WaitAny).WithName("Join");
        }
    }
}