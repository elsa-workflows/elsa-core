using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Elsa.Workflows.Management.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Elsa.Workflows.Api.ComponentTests.Endpoints.WorkflowDefinitions.Execute;

public class Tests(WorkflowServerTestWebAppFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task Post_ShouldReturnOk_WhenWorkflowDefinitionIsValid()
    {
        // Arrange: Create a HTTP client
        var client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", "48587230567A646D394B435A6277734A-4802fa49-e91e-45e8-b00f-b5492377e20b");

        // Use JsonSerializer to serialize your object to JSON string
        var requestJson = "{}";
        var httpContent = new StringContent(requestJson, Encoding.UTF8, MediaTypeNames.Application.Json);

        // Act: Send a POST request
        using HttpResponseMessage response = await client.PostAsync("/elsa/api/workflow-definitions/hello-world/execute", httpContent);

        // Assert: Check the HTTP status code
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    
        // Assert: Check the content of the response if needed
        // var content = await response.Content.ReadAsStringAsync();
        // Assert.Equal(expectedContent, content);
    }
}