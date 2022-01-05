using System;
using Elsa.Activities.Console;
using Elsa.Activities.RabbitMq;
using Elsa.Builders;
using Microsoft.Extensions.Configuration;

namespace Elsa.Samples.RabbitMqWorker.Workflows
{
    public class SendAndReceiveWorkflow : IWorkflow
    {
        private readonly string _connectionString;
        public SendAndReceiveWorkflow(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("RabbitMq");
        }

        public void Build(IWorkflowBuilder builder) => builder
            .WriteLine(ctx =>
            {
                var correlationId = Guid.NewGuid().ToString("n");
                ctx.WorkflowInstance.CorrelationId = correlationId;
             
                return $"Start! - correlationId: {correlationId}";
            })
            .SendTopicMessage(_connectionString, "Greetings", "Greetings from RabbitMQ")
            .MessageReceived(_connectionString, "Greetings")         
            .WriteLine(ctx => "End: " + (string?)ctx.Input);

    }
}