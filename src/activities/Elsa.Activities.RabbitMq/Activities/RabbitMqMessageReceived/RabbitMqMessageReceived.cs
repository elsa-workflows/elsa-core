using Elsa.Activities.RabbitMq.Configuration;
using Elsa.Activities.RabbitMq.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Rebus.Messages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elsa.Activities.RabbitMq
{
    [Trigger(
        Category = "RabbitMQ",
        DisplayName = "RabbitMQ Message Received",
        Description = "Triggers when RabbitMQ message matching specified routing key is received",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class RabbitMqMessageReceived : Activity, IRabbitMqActivity
    {
        private readonly IMessageReceiverClientFactory _messageReceiverClientFactory;

        public RabbitMqMessageReceived(IMessageReceiverClientFactory messageReceiverClientFactory)
        {
            _messageReceiverClientFactory = messageReceiverClientFactory;
        }

        [ActivityInput(
            Hint = "Exchange to listen to",
            Order = 1,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string ExchangeName { get; set; } = default!;

        [ActivityInput(
            Hint = "Routing Key",
            Order = 2,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string RoutingKey { get; set; } = default!;

        [ActivityInput(
            Hint = "List of headers that should be present in the message",
            Order = 3,
            UIHint = ActivityInputUIHints.Dictionary,
            DefaultSyntax = SyntaxNames.Json,
            SupportedSyntaxes = new[] { SyntaxNames.Json })]
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        [ActivityInput(
            Hint = "RabbitMQ connection string [amqp://user:pass@host:10000/vhost] - https://www.rabbitmq.com/uri-spec.html",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid },
            Order = 2,
            Category = PropertyCategories.Configuration)]
        public string ConnectionString { get; set; } = default!;


        [ActivityOutput(Hint = "Received message")]
        public object? Output { get; set; }
        
        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context) => context.WorkflowExecutionContext.IsFirstPass ? ExecuteInternalAsync(context) : await SuspendInternalAsync();
        
        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context) => ExecuteInternalAsync(context);
        
        private async ValueTask<IActivityExecutionResult> SuspendInternalAsync()
        {
            await StartClient();

            return Suspend();
        }

        private IActivityExecutionResult ExecuteInternalAsync(ActivityExecutionContext context)
        {
            var message = (TransportMessage)context.Input!;

            var messageBody = System.Text.Encoding.UTF8.GetString(message.Body);

            Output = messageBody;

            context.LogOutputProperty(this, nameof(Output), Output);
            context.JournalData.Add("Headers", message.Headers);

            return Done();
        }
        private async Task StartClient()
        {
            var config = new RabbitMqBusConfiguration(ConnectionString, ExchangeName, RoutingKey, Headers);

            var client = await _messageReceiverClientFactory.GetReceiverAsync(config);
            client.StartClient();
        }
    }
}