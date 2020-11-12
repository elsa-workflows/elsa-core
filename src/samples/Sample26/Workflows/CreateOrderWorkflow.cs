using System;
using System.Net;
using System.Net.Http;
using Elsa.Activities;
using Elsa.Activities.Email.Activities;
using Elsa.Activities.Http.Activities;
using Elsa.Activities.MassTransit.Activities;
using Elsa.Activities.ControlFlow.Activities;
using Elsa.Activities.Workflows.Activities;
using Elsa.Expressions;
using Elsa.Scripting.JavaScript;
using Elsa.Services;
using Elsa.Services.Models;
using Sample26.Messages;

namespace Sample26.Workflows
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
                        activity.ValueExpression = new JavaScriptExpression<object>("lastResult().Body");
                    }
                )
                // Need to ensure that the correlation ID is the same string format that is used by the WorkflowConsumer<T>
                // MassTransit will always use Guid values for correlation ID's, so need to ensure the same string format is used.
                .Then<Correlate>(activity => activity.ValueExpression = new JavaScriptExpression<string>("newGuid()"))
                .Then<SendMassTransitMessage>(activity =>
                    {
                        activity.Message = new JavaScriptExpression<CreateOrder>("return { correlationId: correlationId(), order: order};");
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