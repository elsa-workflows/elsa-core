using Elsa.Activities.Console;
using Elsa.Activities.RabbitMq;
using Elsa.Builders;
using Microsoft.Extensions.Configuration;

namespace Elsa.Samples.RabbitMqWorker.Workflows
{
    public class ConsumerWorkflow : IWorkflow
    {
        private readonly string _connectionString;
        public ConsumerWorkflow(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("RabbitMq");
        }

        public void Build(IWorkflowBuilder builder)
        {
            builder
                .MessageReceived(_connectionString, "Podcasts.Weather")
                .WriteLine(context =>
                {
                    var message = context.GetInput<string>();
                    return $"Received a weather update saying {message}";
                });
        }
    }
}
