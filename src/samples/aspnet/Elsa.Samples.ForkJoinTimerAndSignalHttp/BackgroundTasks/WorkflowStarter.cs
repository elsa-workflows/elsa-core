using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Samples.ForkJoinTimerAndSignalHttp.BackgroundTasks
{
    /// <summary>
    /// A simple worker that starts a workflow
    /// </summary>
    public class WorkflowStarter<T> : IHostedService where T : IWorkflow
    {
        private readonly IServiceProvider _serviceProvider;

        public WorkflowStarter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var workflowRunner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();
            await workflowRunner.RunWorkflowAsync<T>(correlationId: Guid.NewGuid().ToString("N"), cancellationToken: cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}