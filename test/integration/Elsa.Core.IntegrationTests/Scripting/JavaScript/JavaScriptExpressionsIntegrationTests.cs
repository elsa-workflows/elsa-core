using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Primitives;
using Elsa.Core.IntegrationTests.Autofixture;
using Elsa.Models;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Services.Models;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.Helpers;
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