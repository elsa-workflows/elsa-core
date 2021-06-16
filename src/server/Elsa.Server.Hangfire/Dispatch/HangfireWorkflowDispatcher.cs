using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Server.Hangfire.Jobs;
using Elsa.Services.Dispatch;
using Hangfire;
using Hangfire.States;

namespace Elsa.Server.Hangfire.Dispatch
{
    public class HangfireWorkflowDispatcher : IWorkflowDefinitionDispatcher, IWorkflowInstanceDispatcher, IWorkflowDispatcher
    {
        private readonly IBackgroundJobClient _jobClient;
        public HangfireWorkflowDispatcher(IBackgroundJobClient jobClient) => _jobClient = jobClient;

        public Task DispatchAsync(ExecuteWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
        {
            EnqueueJob<WorkflowDefinitionJob>(x => x.ExecuteAsync(request, CancellationToken.None), QueueNames.WorkflowDefinitions);
            return Task.CompletedTask;
        }

        public Task DispatchAsync(ExecuteWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
        {
            EnqueueJob<WorkflowInstanceJob>(x => x.ExecuteAsync(request, CancellationToken.None), QueueNames.WorkflowInstances);
            return Task.CompletedTask;
        }

        public Task DispatchAsync(TriggerWorkflowsRequest request, CancellationToken cancellationToken = default)
        {
            EnqueueJob<CorrelatedWorkflowDefinitionJob>(x => x.ExecuteAsync(request, CancellationToken.None), QueueNames.CorrelatedWorkflows);
            return Task.CompletedTask;
        }

        private void EnqueueJob<T>(Expression<Func<T, Task>> job, string queueName) => _jobClient.Create(job, new EnqueuedState(queueName));
    }
}