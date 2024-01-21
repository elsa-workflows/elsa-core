using System.Threading;
using System.Threading.Tasks;
using Elsa.Core.IntegrationTests.Autofixture;
using Elsa.Core.IntegrationTests.Workflows;
using Elsa.Persistence;
using Elsa.Services;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Elsa.Core.IntegrationTests.Persistence.YesSql
{
    public class PostgresSqlYesSqlIntegrationTests
    {
        [Theory(DisplayName = "A persistable workflow instance with default persistence behaviour should be persisted-to and readable-from a YesSQL/PostgreSql store after being run"), AutoMoqData]
        public async Task APersistableWorkflowInstanceWithDefaultPersistenceBehaviourShouldBeRoundTrippable([WithPersistableWorkflow,WithPostgresYesSql] ElsaHostBuilderBuilder hostBuilderBuilder)
        {
            var hostBuilder = hostBuilderBuilder.GetHostBuilder();
            hostBuilder.ConfigureServices((ctx, services) => services.AddHostedService<HostedWorkflowRunner>());
            var host = await hostBuilder.StartAsync();
        }

        class HostedWorkflowRunner : IHostedService
        {
            private readonly IServiceScopeFactory _scopeFactory;

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                await using var serviceScope = _scopeFactory.CreateAsyncScope();
                var runWorkflowResult = await serviceScope
                    .ServiceProvider
                    .GetRequiredService<IBuildsAndStartsWorkflow>()
                    .BuildAndStartWorkflowAsync<PersistableWorkflow>(cancellationToken: cancellationToken);
                var instance = runWorkflowResult.WorkflowInstance!;
                var retrievedInstance = await serviceScope
                    .ServiceProvider
                    .GetRequiredService<IWorkflowInstanceStore>()
                    .FindByIdAsync(instance.Id, cancellationToken);

                Assert.NotNull(retrievedInstance);
            }

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            
            public HostedWorkflowRunner(IServiceScopeFactory scopeFactory)
            {
                _scopeFactory = scopeFactory ?? throw new System.ArgumentNullException(nameof(scopeFactory));
            }
        }
    }
}