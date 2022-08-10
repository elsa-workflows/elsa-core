using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.HostedServices;
using Elsa.Samples.RunChildWorkflowWorker.Workflows;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.RunChildWorkflowWorker.HostedServices
{
    /// <summary>
    /// Runs the parent workflow.
    /// </summary>
    public class RunParentWorkflow : IElsaHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public RunParentWorkflow(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var workflowRunner = scope.ServiceProvider.GetRequiredService<IBuildsAndStartsWorkflow>();
            await workflowRunner.BuildAndStartWorkflowAsync<ParentWorkflow>(cancellationToken: cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}