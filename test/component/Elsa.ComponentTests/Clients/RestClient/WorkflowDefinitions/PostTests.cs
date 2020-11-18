using System.Threading.Tasks;
using AutoFixture;
using Elsa.Activities.Console;
using Elsa.Client.Models;
using Elsa.ComponentTests.Helpers;
using Xunit;

namespace Elsa.ComponentTests.Clients.RestClient.WorkflowDefinitions
{
    [Collection(ComponentTestsCollection.Name)]
    public class PostTests : ElsaClientTestBase
    {
        public PostTests(ElsaHostApplicationFactory hostApplicationFactory) : base(hostApplicationFactory)
        {
        }

        [Fact(DisplayName = "Posting a new workflow definition returns HTTP 201.")]
        public async Task Post01()
        {
            var request = CreateWorkflowDefinition();
            var workflowDefinition = await ElsaClient.WorkflowDefinitions.PostAsync(request);
            Assert.Equal(request.Name, workflowDefinition.Name);
        }

        private WorkflowDefinition CreateWorkflowDefinition()
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

            return Fixture.Build<WorkflowDefinition>()
                .With(x => x.Activities, activities)
                .With(x => x.Connections, connections)
                .Create();
        }
    }
}