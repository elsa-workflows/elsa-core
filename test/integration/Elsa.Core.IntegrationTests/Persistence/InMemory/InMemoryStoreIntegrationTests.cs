using Xunit;
using System.Threading.Tasks;
using Elsa.Core.IntegrationTests.Autofixture;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using Elsa.Services;
using Elsa.Core.IntegrationTests.Workflows;
using Elsa.Persistence;

namespace Elsa.Core.IntegrationTests.Persistence.InMemory
{
    public class InMemoryStoreIntegrationTests
    {
        [Theory(DisplayName = "A saved workflow instance should be persisted-to and readable-from an in-memory store after being run"), AutoMoqData]
        public async Task ASavedWorkflowInstanceShouldBeRoundTrippable([HostBuilderWithElsaSampleWorkflow] IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices((ctx, services) => services.AddHostedService<HostedWorkflowRunner>());
            var host = await hostBuilder.StartAsync();
        }

        class HostedWorkflowRunner : IHostedService
        {
            readonly IWorkflowRunner workflowRunner;
            readonly IWorkflowInstanceStore instanceStore;

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                var instance = await workflowRunner.RunWorkflowAsync<SampleWorkflow>();
                var retrievedInstance = await instanceStore.FindByIdAsync(instance.Id);

                // An instance should totally be retrieved from the store
                Assert.NotNull(retrievedInstance);
                // Because we are in-memory, it should be a reference to the same instance
                Assert.Same(instance, retrievedInstance);
            }

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

            public HostedWorkflowRunner(IWorkflowRunner workflowRunner, IWorkflowInstanceStore instanceStore)
            {
                this.workflowRunner = workflowRunner ?? throw new System.ArgumentNullException(nameof(workflowRunner));
                this.instanceStore = instanceStore ?? throw new System.ArgumentNullException(nameof(instanceStore));
            }
        }
    }
}