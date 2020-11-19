using System.Threading.Tasks;
using AutoFixture;
using Elsa.Activities.Console;
using Elsa.Client.Models;
using Elsa.ComponentTests.Helpers;
using Xunit;

namespace Elsa.ComponentTests.Clients.RestClient.WorkflowDefinitions
{
    [Collection(ComponentTestsCollection.Name)]
    public class SaveTests : ElsaClientTestBase
    {
        public SaveTests(ElsaHostApplicationFactory hostApplicationFactory) : base(hostApplicationFactory)
        {
        }

        [Fact(DisplayName = "Saving a new workflow definition returns HTTP 201.")]
        public async Task Post01()
        {
            var request = CreateSaveWorkflowRequest();
            var workflowDefinition = await ElsaClient.WorkflowDefinitions.SaveAsync(request);
            Assert.Equal(request.Name, workflowDefinition.Name);
        }

        private SaveWorkflowDefinitionRequest CreateSaveWorkflowRequest()
        {
            var writeLine = new ActivityDefinition
            {
                ActivityId = Fixture.Create<string>(),
                Type = nameof(WriteLine),
                Properties = new ActivityDefinitionProperties
                {
                    [nameof(WriteLine.Text)] = ActivityDefinitionPropertyValue.Literal("Hello World!")
                }
            };

            var readLine = new ActivityDefinition {ActivityId = Fixture.Create<string>(), Type = nameof(ReadLine)};
            var activities = new[] {writeLine, readLine};
            var connections = new[] {new ConnectionDefinition(writeLine.ActivityId, readLine.ActivityId, OutcomeNames.Done)};

            return Fixture.Build<SaveWorkflowDefinitionRequest>()
                .With(x => x.Activities, activities)
                .With(x => x.Connections, connections)
                .Create();
        }
    }
}