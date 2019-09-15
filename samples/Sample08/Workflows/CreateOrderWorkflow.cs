using System;
using System.Net;
using System.Net.Http;
using Elsa.Activities.Email.Activities;
using Elsa.Activities.Http.Activities;
using Elsa.Activities.MassTransit.Activities;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Expressions;
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
                .StartWith<HttpRequestEvent>(
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
                        activity.Expression = new JavaScriptExpression<object>("lastResult().ParsedContent");
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
                            .Then<HttpResponseAction>(
                                activity =>
                                {
                                    activity.Content = new Literal("<h1>Order Received</h1><p>Your order has been received. Waiting for shipment.</p>");
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
                                    activity.From = new Literal("shipment@acme.com");
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