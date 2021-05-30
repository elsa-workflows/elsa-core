using System.Threading.Tasks;
using Elsa.Activities.Conductor.Models;
using Elsa.Activities.Conductor.Services;
using Rebus.Handlers;

namespace Elsa.Activities.Conductor.Consumers
{
    public class SendCommandConsumer : IHandleMessages<SendCommandModel>
    {
        private readonly RemoteApplicationClient _remoteApplicationClient;
        public SendCommandConsumer(RemoteApplicationClient remoteApplicationClient) => _remoteApplicationClient = remoteApplicationClient;
        public async Task Handle(SendCommandModel message) => await _remoteApplicationClient.SendCommandAsync(message.Command, message.Payload);
    }
}