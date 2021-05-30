using System.Threading.Tasks;
using Elsa.Activities.Conductor.Models;
using Elsa.Activities.Conductor.Services;
using Rebus.Handlers;

namespace Elsa.Activities.Conductor.Consumers
{
    public class SendCommandConsumer : IHandleMessages<SendCommandModel>
    {
        private readonly ApplicationCommandsClient _applicationCommandsClient;
        public SendCommandConsumer(ApplicationCommandsClient applicationCommandsClient) => _applicationCommandsClient = applicationCommandsClient;
        public async Task Handle(SendCommandModel message) => await _applicationCommandsClient.SendCommandAsync(message);
    }
}