using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Console;
using Elsa.Activities.Primitives;
using Elsa.Core.IntegrationTests.Autofixture;
using Elsa.Models;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Services.Models;
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

        class HostedWorkflowRunner : IHostedService
        {
            readonly IWorkflowRunner workflowRunner;
            readonly IContentSerializer serializer;
            readonly IWorkflowBlueprintMaterializer materializer;

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                var workflow = await GetWorkflowBlueprintAsync(GetWorkflowDefinition());
                var workflowInstance = await workflowRunner.RunWorkflowAsync(workflow);

                Assert.NotNull(workflowInstance);
                Assert.Null(workflowInstance.Fault);
            }

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

            async Task<IWorkflowBlueprint> GetWorkflowBlueprintAsync(WorkflowDefinition workflowDefinition)
            {
                var json = serializer.Serialize(workflowDefinition);
                var deserializedWorkflowDefinition = serializer.Deserialize<WorkflowDefinition>(json);
                return await materializer.CreateWorkflowBlueprintAsync(deserializedWorkflowDefinition);
            }

            WorkflowDefinition GetWorkflowDefinition()
            {
                return  new WorkflowDefinition
                {
                    Id = "1",
                    DefinitionId = "SampleWorkflow",
                    Version = 1,
                    IsPublished = true,
                    IsLatest = true,
                    PersistenceBehavior = WorkflowPersistenceBehavior.Suspended,
                    Activities = new[]
                    {
                        new ActivityDefinition {
                            ActivityId = "1",
                            Type = nameof(SetVariable),
                            Properties = new List<ActivityDefinitionProperty>
                            {
                                ActivityDefinitionProperty.Literal(nameof(SetVariable.VariableName), "MyVariable"),
                                ActivityDefinitionProperty.JavaScript(nameof(SetVariable.Value), @"JSON.parse(""{\""foo\"":\""bar\""}"")"),
                            }
                        },
                        new ActivityDefinition {
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

            public HostedWorkflowRunner(IWorkflowRunner workflowRunner, IContentSerializer serializer, IWorkflowBlueprintMaterializer materializer)
            {
                this.workflowRunner = workflowRunner ?? throw new System.ArgumentNullException(nameof(workflowRunner));
                this.serializer = serializer ?? throw new System.ArgumentNullException(nameof(serializer));
                this.materializer = materializer ?? throw new System.ArgumentNullException(nameof(materializer));
            }
        }
    }
}