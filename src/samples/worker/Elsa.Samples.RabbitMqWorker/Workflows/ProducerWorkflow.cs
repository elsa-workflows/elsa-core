using Elsa.Activities.Console;
using Elsa.Activities.RabbitMq;
using Elsa.Activities.Temporal;
using Elsa.Builders;
using Microsoft.Extensions.Configuration;
using NodaTime;

namespace Elsa.Samples.RabbitMqWorker.Workflows
{
    public class ProducerWorkflow : IWorkflow
    {
        private readonly string _connectionString;
        public ProducerWorkflow(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("RabbitMq");
        }

        public void Build(IWorkflowBuilder builder)
        {
            builder
                .Timer(Duration.FromSeconds(10))
                .WriteLine("Sending a weather update with the \"Podcasts.Weather\" topic.")
                .SendTopicMessage(_connectionString, "Podcasts.Weather", "Cloudy with a chance of meatballs");
        }
    }
}