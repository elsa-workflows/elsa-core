using System.Threading.Tasks;
using Elsa.Activities.Conductor.Models;
using Elsa.Activities.Conductor.Services;
using Rebus.Handlers;

namespace Elsa.Activities.Conductor.Consumers
{
    public class RunTaskConsumer : IHandleMessages<RunTaskModel>
    {
        private readonly ApplicationTasksClient _applicationTasksClient;
        public RunTaskConsumer(ApplicationTasksClient applicationTasksClient) => _applicationTasksClient = applicationTasksClient;
        public async Task Handle(RunTaskModel message) => await _applicationTasksClient.RunTaskAsync(message);
    }
}