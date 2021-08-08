using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Scripting.Liquid.Extensions;
using Elsa.Services;
using Elsa.Testing.Shared.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Core.IntegrationTests.Scripting.Liquid
{
    [CollectionDefinition(nameof(LiquidExpressionIntegrationTests), DisableParallelization = true)]
    public class LiquidExpressionIntegrationTests
    {
        [Fact(DisplayName = "A Liquid expressions cannot access configuration by default")]
        public async Task LiquidExpressionsCannotAccessConfigurationByDefault()
        {
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(_ => new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string>
                    {
                        { "SomeSecret", "I am a secret" },
                    })
                    .Build())
                .AddSingleton<AssertableActivityState>()
                .AddElsa(elsa => elsa
                    .AddActivity<AddExpressionToActivityState>()
                    .AddWorkflow<ConfigurationAccessWorkflow>())
                .BuildServiceProvider();

            var workflowStarter = services.GetRequiredService<IBuildsAndStartsWorkflow>();
            var activityState = services.GetRequiredService<AssertableActivityState>();

            await workflowStarter.BuildAndStartWorkflowAsync<ConfigurationAccessWorkflow>();

            Assert.Single(activityState.Messages, "");

            await services.DisposeAsync();
        }

        [Fact(DisplayName = "B Configuration access can be enabled for Liquid expressions")]
        public async Task ConfigureAccessCanBeEnabledForLiquidExpressions()
        {
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(_ => new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string>
                    {
                        { "SomeSecret", "I am a secret" },
                    })
                    .Build())
                .AddSingleton<AssertableActivityState>()
                .AddElsa(elsa => elsa
                    .AddActivity<AddExpressionToActivityState>()
                    .AddWorkflow<ConfigurationAccessWorkflow>())
                .EnableLiquidConfigurationAccess()
                .BuildServiceProvider();

            var workflowStarter = services.GetRequiredService<IBuildsAndStartsWorkflow>();
            var activityState = services.GetRequiredService<AssertableActivityState>();

            await workflowStarter.BuildAndStartWorkflowAsync<ConfigurationAccessWorkflow>();

            Assert.Single(activityState.Messages, "I am a secret");

            await services.DisposeAsync();
        }

        private class ConfigurationAccessWorkflow : IWorkflow
        {
            public void Build(IWorkflowBuilder builder)
            {
                builder.StartWith<AddExpressionToActivityState>(setup => setup
                    .Set(x => x.Expression, _ => "{{Configuration.SomeSecret}}")
                    .Set(x => x.ExpressionSyntax, _ => "Liquid"));
            }
        }
    }
}