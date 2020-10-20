using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AutoFixture;
using Elsa.Activities.Console;
using Elsa.ComponentTests.Helpers;
using Elsa.Core.IntegrationTests.Helpers;
using Elsa.Models;
using Elsa.Server.Api.Endpoints.WorkflowDefinitions;
using Elsa.Testing.Shared.AutoFixture;
using Xunit;

namespace Elsa.ComponentTests.Endpoints.WorkflowDefinitions
{
    [Collection(ComponentTestsCollection.Name)]
    public class PostTests : IDisposable
    {
        private readonly IFixture _fixture;
        private readonly TemporaryFolder _tempFolder;
        private readonly ElsaHostApplicationFactory _hostApplicationFactory;
        private readonly HttpClient _httpClient;

        public PostTests(ElsaHostApplicationFactory hostApplicationFactory)
        {
            _fixture = new Fixture().Customize(new NodaTimeCustomization());
            _tempFolder = new TemporaryFolder();
            _hostApplicationFactory = hostApplicationFactory;
            hostApplicationFactory.SetDbConnectionString($@"Data Source={_tempFolder.Folder}elsa.db;Cache=Shared");
            _httpClient = hostApplicationFactory.CreateClient();
        }

        [Fact(DisplayName = "Posting a new workflow definition returns HTTP 201.")]
        public async Task Post01()
        {
            var request = CreateWorkflowDefinitionRequest();
            var response = await _httpClient.PostJsonAsync("/v1/workflow-definitions", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        private SaveWorkflowDefinitionRequest CreateWorkflowDefinitionRequest()
        {
            var writeLine = new ActivityDefinition
            {
                Id = _fixture.Create<string>(),
                Type = nameof(WriteLine),
                Properties = new ActivityDefinitionProperties
                {
                    [nameof(WriteLine.Text)] = ActivityDefinitionPropertyValue.Literal("Hello World!")
                }
            };

            var readLine = new ActivityDefinition { Id = _fixture.Create<string>(), Type = nameof(ReadLine) };
            var activities = new[] { writeLine, readLine };
            var connections = new[] { new ConnectionDefinition(writeLine.Id, readLine.Id, OutcomeNames.Done) };

            return _fixture.Build<SaveWorkflowDefinitionRequest>()
                .With(x => x.Activities, activities)
                .With(x => x.Connections, connections)
                .Create();
        }

        private string CreateWorkflowDefinitionJson()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _hostApplicationFactory.Dispose();
            _tempFolder.Dispose();
        }
    }
}