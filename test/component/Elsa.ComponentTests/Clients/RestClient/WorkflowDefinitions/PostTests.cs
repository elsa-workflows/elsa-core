using System;
using System.Net;
using System.Threading.Tasks;
using AutoFixture;
using Elsa.Activities.Console;
using Elsa.Client;
using Elsa.Client.Extensions;
using Elsa.Client.Models;
using Elsa.ComponentTests.Helpers;
using Elsa.Testing.Shared.AutoFixture;
using Elsa.Testing.Shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.ComponentTests.Clients.RestClient.WorkflowDefinitions
{
    [Collection(ComponentTestsCollection.Name)]
    public class PostTests : IDisposable
    {
        private readonly IFixture _fixture;
        private readonly TemporaryFolder _tempFolder;
        private readonly ElsaHostApplicationFactory _hostApplicationFactory;
        private readonly IElsaClient _elsaClient;

        public PostTests(ElsaHostApplicationFactory hostApplicationFactory)
        {
            _fixture = new Fixture().Customize(new NodaTimeCustomization());
            _tempFolder = new TemporaryFolder();
            _hostApplicationFactory = hostApplicationFactory;
            hostApplicationFactory.SetDbConnectionString($@"Data Source={_tempFolder.Folder}elsa.db;Cache=Shared");
            var httpClient = hostApplicationFactory.CreateClient();

            var services = new ServiceCollection().AddElsaClient(options => options.ServerUrl = httpClient.BaseAddress!).BuildServiceProvider();
            _elsaClient = services.GetRequiredService<IElsaClient>();
        }

        [Fact(DisplayName = "Posting a new workflow definition returns HTTP 201.")]
        public async Task Post01()
        {
            var request = CreateWorkflowDefinitionRequest();
            var workflowDefinition = await _elsaClient.WorkflowDefinitions.PostAsync(request);
            Assert.Equal(request.Name, workflowDefinition.Name);
        }

        private PostWorkflowDefinitionRequest CreateWorkflowDefinitionRequest()
        {
            var writeLine = new ActivityDefinition
            {
                ActivityId = _fixture.Create<string>(),
                Type = nameof(WriteLine),
                Properties = new ActivityDefinitionProperties
                {
                    [nameof(WriteLine.Text)] = ActivityDefinitionPropertyValue.Literal("Hello World!")
                }
            };

            var readLine = new ActivityDefinition { ActivityId = _fixture.Create<string>(), Type = nameof(ReadLine) };
            var activities = new[] { writeLine, readLine };
            var connections = new[] { new ConnectionDefinition(writeLine.ActivityId, readLine.ActivityId, OutcomeNames.Done) };

            return _fixture.Build<PostWorkflowDefinitionRequest>()
                .With(x => x.Activities, activities)
                .With(x => x.Connections, connections)
                .Create();
        }

        public void Dispose()
        {
            _hostApplicationFactory.Dispose();
            _tempFolder.Dispose();
        }
    }
}