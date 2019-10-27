using System;
using System.Net;
using System.Net.Http;
using Elsa.Activities;
using Elsa.Activities.Email.Activities;
using Elsa.Activities.Http.Activities;
using Elsa.Activities.MassTransit.Activities;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.ControlFlow.Activities;
using Elsa.Expressions;
using Elsa.Scripting.JavaScript;
using Elsa.Services;
using Elsa.Services.Models;
using Sample08.Messages;

namespace Sample08.Workflows
{
    public class CreateOrderWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .StartWith<ReceiveHttpRequest>(
                    activity =>
                    {
                        activity.Method = HttpMethod.Post.Method;
                        activity.Path = new Uri("/orders", UriKind.RelativeOrAbsolute);
                        activity.ReadContent = true;
                    }
                )
                .Then<SetVariable>(
                    activity =>
                    {
                        activity.VariableName = "order";
                        activity.ValueExpression = new JavaScriptExpression<object>("lastResult().Content");
                    }
                )
                .Then<SendMassTransitMessage>(activity =>
                    {
                        activity.Message = new JavaScriptExpression<CreateOrder>("return {order: order};");
                        activity.MessageType = typeof(CreateOrder);
                    }
                )
                .Then<Fork>(
                    activity => activity.Branches = new[] { "Write-Response", "Await-Shipment" },
                    fork =>
                    {
                        fork
                            .When("Write-Response")
                            .Then<WriteHttpResponse>(
                                activity =>
                                {
                                    activity.Content = new LiteralExpression("<h1>Order Received</h1><p>Your order has been received. Waiting for shipment.</p>");
                                    activity.ContentType = "text/html";
                                    activity.StatusCode = HttpStatusCode.Accepted;
                                }
                            );

                        fork
                            .When("Await-Shipment")
                            .Then<ReceiveMassTransitMessage>(activity => activity.MessageType = typeof(OrderShipped))
                            .Then<SendEmail>(
                                activity =>
                                {
                                    activity.From = new LiteralExpression("shipment@acme.com");
                                    activity.To = new JavaScriptExpression<string>("order.customer.email");
                                    activity.Subject = new JavaScriptExpression<string>("`Your order with ID #${order.id} has been shipped!`");
                                    activity.Body = new JavaScriptExpression<string>(
                                        "`Dear ${order.customer.name}, your order has shipped!`"
                                    );
                                }
                            );
                    }
                );
        }
    }
}