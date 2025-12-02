using System.Net;
using System.Text;
using System.Text.Json;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Http;

public class HttpEndpointContentTests(App app) : AppComponentTest(app)
{
    [Fact]
    public async Task JsonContent_ValidJson_ReturnsEchoedJson()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        var testData = new { Name = "John", Age = 30, City = "New York" };
        var jsonContent = JsonSerializer.Serialize(testData);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("test/json-content", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        
        // Verify JSON structure is preserved
        var parsedResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        Assert.True(parsedResponse.TryGetProperty("Name", out var nameProperty));
        Assert.Equal("John", nameProperty.GetString());
    }

    [Fact]
    public async Task JsonContent_InvalidJson_ReturnsBadRequest()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        var invalidJson = "{ \"name\": \"John\", invalid }";
        var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("test/json-content", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task JsonContent_EmptyBody_ReturnsNoContentMessage()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        var content = new StringContent("", Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("test/json-content", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("No content received", responseContent);
    }

    [Fact]
    public async Task FormData_ValidFormData_ReturnsExtractedFields()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        var formData = new List<KeyValuePair<string, string>>
        {
            new("name", "John Doe"),
            new("email", "john@example.com")
        };
        var content = new FormUrlEncodedContent(formData);

        // Act
        var response = await client.PostAsync("test/form-data", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Name: John Doe", responseContent);
        Assert.Contains("Email: john@example.com", responseContent);
    }

    [Fact]
    public async Task FormData_MissingFields_ReturnsUnknownValues()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        var formData = new List<KeyValuePair<string, string>>
        {
            new("other", "value")
        };
        var content = new FormUrlEncodedContent(formData);

        // Act
        var response = await client.PostAsync("test/form-data", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Name: unknown", responseContent);
        Assert.Contains("Email: unknown", responseContent);
    }

    [Fact]
    public async Task FormData_EmptyForm_ReturnsNoFormDataMessage()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        var content = new StringContent("", Encoding.UTF8, "text/plain");

        // Act
        var response = await client.PostAsync("test/form-data", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("No form data received", responseContent);
    }
}
