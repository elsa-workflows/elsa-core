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
        var testData = new { Name = "John", Age = 30, City = "New York" };
        var jsonContent = JsonSerializer.Serialize(testData);

        // Act
        var response = await PostJsonContentAsync(jsonContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        
        // Verify JSON structure is preserved
        var parsedResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        Assert.True(parsedResponse.TryGetProperty("Name", out var nameProperty));
        Assert.Equal("John", nameProperty.GetString());
    }

    [Theory]
    [InlineData("{ \"name\": \"John\", invalid }", HttpStatusCode.BadRequest)]
    [InlineData("", HttpStatusCode.OK, "No content received")]
    public async Task JsonContent_InvalidOrEmpty_ReturnsExpectedResponse(
        string jsonContent, 
        HttpStatusCode expectedStatusCode, 
        string? expectedContentFragment = null)
    {
        // Act
        var response = await PostJsonContentAsync(jsonContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(expectedStatusCode, response.StatusCode);
        if (expectedContentFragment != null)
        {
            Assert.Contains(expectedContentFragment, responseContent);
        }
    }

    [Theory]
    [InlineData("John Doe", "john@example.com", "Name: John Doe", "Email: john@example.com")]
    [InlineData("Jane Smith", "jane@test.org", "Name: Jane Smith", "Email: jane@test.org")]
    public async Task FormData_ValidData_ReturnsExtractedFields(
        string name, 
        string email, 
        string expectedNameFragment, 
        string expectedEmailFragment)
    {
        // Arrange
        var formData = new List<KeyValuePair<string, string>>
        {
            new("name", name),
            new("email", email)
        };

        // Act
        var response = await PostFormDataAsync(formData);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(expectedNameFragment, responseContent);
        Assert.Contains(expectedEmailFragment, responseContent);
    }

    [Fact]
    public async Task FormData_MissingFields_ReturnsUnknownValues()
    {
        // Arrange
        var formData = new List<KeyValuePair<string, string>>
        {
            new("other", "value")
        };

        // Act
        var response = await PostFormDataAsync(formData);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        AssertOkResponseContains(response, responseContent, "Name: unknown", "Email: unknown");
    }

    [Fact]
    public async Task FormData_EmptyForm_ReturnsNoFormDataMessage()
    {
        // Act
        var response = await PostEmptyFormAsync();
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        AssertOkResponseContains(response, responseContent, "No form data received");
    }

    private async Task<HttpResponseMessage> PostJsonContentAsync(string jsonContent)
    {
        var client = WorkflowServer.CreateHttpWorkflowClient();
        using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        return await client.PostAsync("test/json-content", content);
    }

    private async Task<HttpResponseMessage> PostFormDataAsync(IEnumerable<KeyValuePair<string, string>> formData)
    {
        var client = WorkflowServer.CreateHttpWorkflowClient();
        using var content = new FormUrlEncodedContent(formData);
        return await client.PostAsync("test/form-data", content);
    }

    private async Task<HttpResponseMessage> PostEmptyFormAsync()
    {
        var client = WorkflowServer.CreateHttpWorkflowClient();
        using var content = new StringContent("", Encoding.UTF8, "text/plain");
        return await client.PostAsync("test/form-data", content);
    }

    private static void AssertOkResponseContains(HttpResponseMessage response, string responseContent, params string[] expectedFragments)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        foreach (var fragment in expectedFragments)
        {
            Assert.Contains(fragment, responseContent);
        }
    }
}
