using System.Linq;
using Hangfire;
using NetBox.Extensions;

namespace Elsa.Server.Hangfire.Extensions
{
    public static class BackgroundJobServerOptionsExtensions
    {
        public static BackgroundJobServerOptions ConfigureForElsaDispatchers(this BackgroundJobServerOptions options)
        {
            var queues = options.Queues.ToHashSet();
            queues.AddRange(new[] { QueueNames.WorkflowDefinitions, QueueNames.WorkflowInstances, QueueNames.CorrelatedWorkflows });
            options.Queues = queues.ToArray();

            return options;
        }
    }
}