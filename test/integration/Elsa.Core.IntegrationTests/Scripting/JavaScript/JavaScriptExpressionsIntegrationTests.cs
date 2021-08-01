using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Primitives;
using Elsa.Builders;
using Elsa.Core.IntegrationTests.Autofixture;
using Elsa.Models;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Services.Models;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Elsa.Core.IntegrationTests.Scripting.JavaScript
{
    public class JavaScriptExpressionsIntegrationTests
    {
        [Theory(DisplayName = "Running a workflow which uses JSON.stringify upon a JSON.parse'd object should not throw"), AutoMoqData]
        public async Task RunningAWorkflowThatIncludesJsonStringifyAParsedObjectShouldNotThrow([WithJavaScriptExpressions] ElsaHostBuilderBuilder hostBuilderBuilder)
        {
            var hostBuilder = hostBuilderBuilder.GetHostBuilder();
            hostBuilder.ConfigureServices((ctx, services) => services.AddHostedService<HostedWorkflowRunner>());
            var host = await hostBuilder.StartAsync();
        }

        [Fact(DisplayName = "JavaScript cannot access CLR by default")]
        public async Task JavaScriptCannotAccessClrByDefault()
        {
            var services = new ServiceCollection()
                .AddSingleton<AssertableActivityState>()
                .AddElsa(elsa => elsa
                    .AddActivity<AddExpressionToActivityState>()
                    .AddWorkflow<ClrAccessWorkflow>())
                .BuildServiceProvider();

            var workflowStarter = services.GetRequiredService<IBuildsAndStartsWorkflow>();
            var activityState = services.GetRequiredService<AssertableActivityState>();

            await workflowStarter.BuildAndStartWorkflowAsync<ClrAccessWorkflow>();

            Assert.Single(activityState.Messages, "");
        }

        [Fact(DisplayName = "JavaScript can have CLR access enabled")]
        public async Task JavaScriptCanHaveClrAccessEnabled()
        {
            var services = new ServiceCollection()
                .AddSingleton<AssertableActivityState>()
                .AddElsa(elsa => elsa
                    .AddActivity<AddExpressionToActivityState>()
                    .AddWorkflow<ClrAccessWorkflow>())
                .WithJavaScriptOptions(options =>
                {
                    options.AllowClr = true;
                })
                .BuildServiceProvider();

            var workflowStarter = services.GetRequiredService<IBuildsAndStartsWorkflow>();
            var activityState = services.GetRequiredService<AssertableActivityState>();

            await workflowStarter.BuildAndStartWorkflowAsync<ClrAccessWorkflow>();

            Assert.Single(activityState.Messages, "System.Dynamic.ExpandoObject");
        }

        [Fact(DisplayName = "JavaScript cannot access configuration by default")]
        public async Task JavaScriptCannotAccessConfigurationByDefault()
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
        }

        [Fact(DisplayName = "JavaScript can have configuration access enabled")]
        public async Task JavaScriptHaveConfigurationAccessEnabled()
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
                .WithJavaScriptOptions(x =>
                {
                    x.EnableConfigurationAccess = true;
                })
                .BuildServiceProvider();

            var workflowStarter = services.GetRequiredService<IBuildsAndStartsWorkflow>();
            var activityState = services.GetRequiredService<AssertableActivityState>();

            await workflowStarter.BuildAndStartWorkflowAsync<ConfigurationAccessWorkflow>();

            Assert.Single(activityState.Messages, "I am a secret");
        }

        private class ConfigurationAccessWorkflow : IWorkflow
        {
            public void Build(IWorkflowBuilder builder)
            {
                builder.StartWith<AddExpressionToActivityState>(setup => setup
                    .Set(x => x.Expression, _ => "getConfig('SomeSecret')")
                    .Set(x => x.ExpressionSyntax, _ => "JavaScript"));
            }
        }

        private class ClrAccessWorkflow : IWorkflow
        {
            public void Build(IWorkflowBuilder builder)
            {
                builder.StartWith<AddExpressionToActivityState>(setup => setup
                    .Set(x => x.Expression, _ => "System.Console.WriteLine")
                    .Set(x => x.ExpressionSyntax, _ => "JavaScript"));
            }
        }
        
        private class HostedWorkflowRunner : IHostedService
        {
            private readonly IStartsWorkflow _workflowRunner;
            private readonly IContentSerializer _serializer;
            private readonly IWorkflowBlueprintMaterializer _materializer;

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                var workflow = await GetWorkflowBlueprintAsync(GetWorkflowDefinition());
                var runWorkflowResult = await _workflowRunner.StartWorkflowAsync(workflow, cancellationToken: cancellationToken);
                var workflowInstance = runWorkflowResult.WorkflowInstance!;

                Assert.NotNull(workflowInstance);
                Assert.Null(workflowInstance.Fault);
            }

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

            private async Task<IWorkflowBlueprint> GetWorkflowBlueprintAsync(WorkflowDefinition workflowDefinition)
            {
                var json = _serializer.Serialize(workflowDefinition);
                var deserializedWorkflowDefinition = _serializer.Deserialize<WorkflowDefinition>(json);
                return await _materializer.CreateWorkflowBlueprintAsync(deserializedWorkflowDefinition);
            }

            private static WorkflowDefinition GetWorkflowDefinition()
            {
                return new()
                {
                    Id = "1",
                    DefinitionId = "SampleWorkflow",
                    Version = 1,
                    IsPublished = true,
                    IsLatest = true,
                    PersistenceBehavior = WorkflowPersistenceBehavior.Suspended,
                    Activities = new[]
                    {
                        new ActivityDefinition
                        {
                            ActivityId = "1",
                            Type = nameof(SetVariable),
                            Properties = new List<ActivityDefinitionProperty>
                            {
                                ActivityDefinitionProperty.Literal(nameof(SetVariable.VariableName), "MyVariable"),
                                ActivityDefinitionProperty.JavaScript(nameof(SetVariable.Value), @"JSON.parse(""{\""foo\"":\""bar\""}"")"),
                            }
                        },
                        new ActivityDefinition
                        {
                            ActivityId = "2",
                            Type = nameof(SetVariable),
                            Properties = new List<ActivityDefinitionProperty>
                            {
                                ActivityDefinitionProperty.Literal(nameof(SetVariable.VariableName), "MyStringifiedVariable"),
                                ActivityDefinitionProperty.JavaScript(nameof(SetVariable.Value), @"JSON.stringify(MyVariable)"),
                            }
                        }
                    },
                    Connections = new[]
                    {
                        new ConnectionDefinition("1", "2", OutcomeNames.Done),
                    }
                };
            }

            public HostedWorkflowRunner(IStartsWorkflow workflowRunner, IContentSerializer serializer, IWorkflowBlueprintMaterializer materializer)
            {
                _workflowRunner = workflowRunner ?? throw new System.ArgumentNullException(nameof(workflowRunner));
                _serializer = serializer ?? throw new System.ArgumentNullException(nameof(serializer));
                _materializer = materializer ?? throw new System.ArgumentNullException(nameof(materializer));
            }
        }
    }
}