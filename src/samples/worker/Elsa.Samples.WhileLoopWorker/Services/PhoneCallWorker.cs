using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.HostedServices;
using Elsa.Samples.WhileLoopWorker.Workflows;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.WhileLoopWorker.Services
{
    /// <summary>
    /// A simple worker that simulates a progressing phone call.
    /// </summary>
    public class PhoneCallWorker : IElsaHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public PhoneCallWorker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var workflowRunner = scope.ServiceProvider.GetRequiredService<IBuildsAndStartsWorkflow>();
            await workflowRunner.BuildAndStartWorkflowAsync<PhoneCallWorkflow>(cancellationToken: cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}