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
        [Theory(DisplayName = "A persistable workflow instance with default persistence behaviour should be persisted-to and readable-from an in-memory store after being run"), AutoMoqData]
        public async Task APersistableWorkflowInstanceWithDefaultPersistanceBehaviourShouldBeRoundTrippable([HostBuilderWithElsaSampleWorkflow] IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices((ctx, services) => {
                services.AddHostedService<HostedWorkflowRunner<PersistableWorkflow>>();
            });
            var host = await hostBuilder.StartAsync();
        }

        [Theory(DisplayName = "A persistable-on-suspend workflow instance should be persisted-to and readable-from an in-memory store after being run"), AutoMoqData]
        public async Task APersistableOnSuspendWorkflowInstanceShouldBeRoundTrippable([HostBuilderWithElsaSampleWorkflow] IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices((ctx, services) => {
                services.AddHostedService<HostedWorkflowRunner<PersistableWorkflow.OnSuspend>>();
            });
            var host = await hostBuilder.StartAsync();
        }

        [Theory(DisplayName = "A persistable-on-activity-executed workflow instance should be persisted-to and readable-from an in-memory store after being run"), AutoMoqData]
        public async Task APersistableOnActivityExecutedWorkflowInstanceShouldBeRoundTrippable([HostBuilderWithElsaSampleWorkflow] IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices((ctx, services) => {
                services.AddHostedService<HostedWorkflowRunner<PersistableWorkflow.OnActivityExecuted>>();
            });
            var host = await hostBuilder.StartAsync();
        }

        [Theory(DisplayName = "A persistable-on-workflow-burst workflow instance should be persisted-to and readable-from an in-memory store after being run"), AutoMoqData]
        public async Task APersistableOnWorkflowBurstWorkflowInstanceShouldBeRoundTrippable([HostBuilderWithElsaSampleWorkflow] IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices((ctx, services) => {
                services.AddHostedService<HostedWorkflowRunner<PersistableWorkflow.OnWorkflowBurst>>();
            });
            var host = await hostBuilder.StartAsync();
        }

        [Theory(DisplayName = "A persistable-on-workflow-pass-completed workflow instance should be persisted-to and readable-from an in-memory store after being run"), AutoMoqData]
        public async Task APersistableOnWorkflowPassCompletedWorkflowInstanceShouldBeRoundTrippable([HostBuilderWithElsaSampleWorkflow] IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices((ctx, services) => {
                services.AddHostedService<HostedWorkflowRunner<PersistableWorkflow.OnWorkflowPassCompleted>>();
            });
            var host = await hostBuilder.StartAsync();
        }

        class HostedWorkflowRunner<TWorkflow> : IHostedService where TWorkflow : PersistableWorkflow
        {
            readonly IWorkflowRunner workflowRunner;
            readonly IWorkflowInstanceStore instanceStore;

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                var instance = await workflowRunner.RunWorkflowAsync<TWorkflow>();
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