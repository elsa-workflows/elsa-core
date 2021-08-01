using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Scripting.Liquid.Extensions;
using Elsa.Services;
using Elsa.Services.Models;
using Elsa.Testing.Shared.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Core.IntegrationTests.Scripting.Liquid
{
    public class LiquidExpressionIntegrationTests
    {
        [Fact]
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
                    .AddActivity<WriteConfigActivity>()
                    .AddWorkflow<ConfigurationAccessWorkflow>())
                .BuildServiceProvider();

            var workflowStarter = services.GetRequiredService<IBuildsAndStartsWorkflow>();
            var activityState = services.GetRequiredService<AssertableActivityState>();

            await workflowStarter.BuildAndStartWorkflowAsync<ConfigurationAccessWorkflow>();

            Assert.Single(activityState.Messages, "Config secret: ");
        }

        [Fact]
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
                    .AddActivity<WriteConfigActivity>()
                    .AddWorkflow<ConfigurationAccessWorkflow>())
                .EnableLiquidConfigurationAccess()
                .BuildServiceProvider();

            var workflowStarter = services.GetRequiredService<IBuildsAndStartsWorkflow>();
            var activityState = services.GetRequiredService<AssertableActivityState>();

            await workflowStarter.BuildAndStartWorkflowAsync<ConfigurationAccessWorkflow>();

            Assert.Single(activityState.Messages, "Config secret: I am a secret");
        }

        private class ConfigurationAccessWorkflow : IWorkflow
        {
            public void Build(IWorkflowBuilder builder)
            {
                builder.StartWith<WriteConfigActivity>();
            }
        }

        private class WriteConfigActivity : Activity
        {
            private IExpressionEvaluator _expressionEvaluator;
            private AssertableActivityState _activityState;

            public WriteConfigActivity(IExpressionEvaluator evaluator, AssertableActivityState activityState)
            {
                _expressionEvaluator = evaluator;
                _activityState = activityState;
            }

            protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
            {
                var liquidExpression = "{{Configuration.SomeSecret}}";
                var expressionResult = await _expressionEvaluator.TryEvaluateAsync<string>(liquidExpression, "Liquid", context);

                _activityState.Messages.Add($"Config secret: {expressionResult.Value ?? ""}");

                return Done();
            }
        }
    }
}