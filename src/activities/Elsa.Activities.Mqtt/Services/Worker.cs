using Elsa.Activities.Mqtt.Activities.MqttMessageReceived;
using Elsa.Activities.Mqtt.Bookmarks;
using Elsa.Activities.Mqtt.Options;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.Mqtt.Services
{
    public class Worker
    {
        private readonly Func<IMqttClientWrapper, Task> _disposeReceiverAction;
        private readonly IMqttClientWrapper _receiverClient;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public Worker(
            IMqttClientWrapper receiverClient,
            IServiceScopeFactory serviceScopeFactory,
            Func<IMqttClientWrapper, Task> disposeReceiverAction)
        {
            _receiverClient = receiverClient;
            _serviceScopeFactory = serviceScopeFactory;
            _disposeReceiverAction = disposeReceiverAction;

            _receiverClient.SetMessageHandlerAsync(OnMessageReceived);
        }

        public void Ping()
        {
            _receiverClient.SetMessageHandlerAsync(OnMessageReceived);
        }

        private string ActivityType => nameof(MqttMessageReceived);
        public async ValueTask DisposeAsync() => await _disposeReceiverAction(_receiverClient);

        private IBookmark CreateBookmark(MqttClientOptions options) => new MessageReceivedBookmark(options.Topic, options.Host, options.Port, options.Username, options.Password, options.QualityOfService);

        private async Task TriggerWorkflowsAsync(MqttApplicationMessage message, CancellationToken cancellationToken)
        {
            var bookmark = CreateBookmark(_receiverClient.Options);
            var launchContext = new WorkflowsQuery(ActivityType, bookmark);
            using var scope = _serviceScopeFactory.CreateScope();
            var workflowLaunchpad = scope.ServiceProvider.GetRequiredService<IWorkflowLaunchpad>();
            await workflowLaunchpad.CollectAndDispatchWorkflowsAsync(launchContext, new WorkflowInput(message), cancellationToken);
        }

        private async Task OnMessageReceived(MqttApplicationMessage message) => await TriggerWorkflowsAsync(message, CancellationToken.None);

    }
}