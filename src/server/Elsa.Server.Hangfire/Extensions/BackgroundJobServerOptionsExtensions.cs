using System;
using System.Linq;
using Elsa.Services;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using NetBox.Extensions;

namespace Elsa.Server.Hangfire.Extensions
{
    public static class BackgroundJobServerOptionsExtensions
    {
        public static BackgroundJobServerOptions ConfigureForElsaDispatchers(this BackgroundJobServerOptions options, IServiceProvider serviceProvider)
        {
            var queues = options.Queues.ToHashSet();
            
            // Add default worker queues.
            queues.AddRange(new[] { QueueNames.CorrelatedWorkflows });
            
            // Add queue variations based on workflow channels, if any.
            var elsaOptions = serviceProvider.GetRequiredService<ElsaOptions>();
            var channels = elsaOptions.WorkflowChannelOptions.Channels;

            foreach (var channel in channels)
            {
                queues.Add(ElsaOptions.FormatChannelQueueName<ExecuteWorkflowDefinitionRequest>(channel));
                queues.Add(ElsaOptions.FormatChannelQueueName<ExecuteWorkflowInstanceRequest>(channel));
            }

            options.Queues = queues.ToArray();

            return options;
        }
    }
}