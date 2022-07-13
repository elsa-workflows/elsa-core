using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.RabbitMq.Services;
using Elsa.Services;

namespace Elsa.Activities.RabbitMq.StartupTasks
{
    public class StartRabbitMqQueues : IStartupTask
    {
        private readonly IRabbitMqQueueStarter _rabbitMqQueueStarter;
        public StartRabbitMqQueues(IRabbitMqQueueStarter rabbitMqQueueStarter) => _rabbitMqQueueStarter = rabbitMqQueueStarter;
        public int Order => 2000;
        public Task ExecuteAsync(CancellationToken stoppingToken) => _rabbitMqQueueStarter.CreateWorkersAsync(stoppingToken);
    }
}