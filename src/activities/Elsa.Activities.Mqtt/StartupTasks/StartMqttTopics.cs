using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Mqtt.Services;
using Elsa.Services;

namespace Elsa.Activities.Mqtt.StartupTasks
{
    public class StartMqttTopics : IStartupTask
    {
        private readonly IMqttTopicsStarter _mqttTopicsStarter;
        public StartMqttTopics(IMqttTopicsStarter mqttTopicsStarter) => _mqttTopicsStarter = mqttTopicsStarter;
        public int Order => 2000;
        public Task ExecuteAsync(CancellationToken stoppingToken) => _mqttTopicsStarter.CreateWorkersAsync(stoppingToken);
    }
}