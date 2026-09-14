using System.Net;
using System.Text;
using System.Text.Json;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Http;

public class HttpEndpointContentTests(App app) : AppComponentTest(app)
{
    private const string RequestSizeLimitPath = "test/request-size-limit";

    [Test]
    public async Task JsonContent_ValidJson_ReturnsEchoedJson()
    {
        // Arrange
        var testData = new { Name = "John", Age = 30, City = "New York" };
        var jsonContent = JsonSerializer.Serialize(testData);

        // Act
        using var response = await PostJsonContentAsync(jsonContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("application/json");

        // Verify JSON structure is preserved
        var parsedResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        await Assert.That(parsedResponse.TryGetProperty("Name", out var nameProperty)).IsTrue();
        await Assert.That(nameProperty.GetString()).IsEqualTo("John");
    }

    [Test]
    [Arguments("{ \"name\": \"John\", invalid }", HttpStatusCode.BadRequest)]
    [Arguments("", HttpStatusCode.OK, "No content received")]
    public async Task JsonContent_InvalidOrEmpty_ReturnsExpectedResponse(
        string jsonContent, 
        HttpStatusCode expectedStatusCode, 
        string? expectedContentFragment = null)
    {
        // Act
        using var response = await PostJsonContentAsync(jsonContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(expectedStatusCode);
        if (expectedContentFragment != null)
        {
            await Assert.That(responseContent).Contains(expectedContentFragment);
        }
    }

    [Test]
    [Arguments("John Doe", "john@example.com", "Name: John Doe", "Email: john@example.com")]
    [Arguments("Jane Smith", "jane@test.org", "Name: Jane Smith", "Email: jane@test.org")]
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
        using var response = await PostFormDataAsync(formData);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(responseContent).Contains(expectedNameFragment);
        await Assert.That(responseContent).Contains(expectedEmailFragment);
    }

    [Test]
    public async Task FormData_MissingFields_ReturnsUnknownValues()
    {
        // Arrange
        var formData = new List<KeyValuePair<string, string>>
        {
            new("other", "value")
        };

        // Act
        using var response = await PostFormDataAsync(formData);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        await AssertOkResponseContains(response, responseContent, "Name: unknown", "Email: unknown");
    }

    [Test]
    public async Task FormData_EmptyForm_ReturnsNoFormDataMessage()
    {
        // Act
        using var response = await PostEmptyFormAsync();
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        await AssertOkResponseContains(response, responseContent, "No form data received");
    }

    [Test]
    public async Task RequestSizeLimit_NoContentLengthOversizedBody_ReturnsPayloadTooLarge()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        using var content = new NoLengthStringContent("0123456789", "text/plain");
        await Assert.That(content.Headers.ContentLength).IsNull();

        // Act
        using var response = await client.PostAsync(RequestSizeLimitPath, content);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.RequestEntityTooLarge);
    }

    [Test]
    public async Task RequestSizeLimit_NoContentLengthSmallBody_ReturnsEchoedContent()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        using var content = new NoLengthStringContent("small", "text/plain");
        await Assert.That(content.Headers.ContentLength).IsNull();

        // Act
        using var response = await client.PostAsync(RequestSizeLimitPath, content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        await AssertOkResponseContains(response, responseContent, "small");
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

    private static async Task AssertOkResponseContains(HttpResponseMessage response, string responseContent, params string[] expectedFragments)
    {
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        foreach (var fragment in expectedFragments)
        {
            await Assert.That(responseContent).Contains(fragment);
        }
    }

    private sealed class NoLengthStringContent : HttpContent
    {
        private readonly string _content;

        public NoLengthStringContent(string content, string mediaType)
        {
            _content = content;
            Headers.ContentType = new(mediaType);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            var bytes = Encoding.UTF8.GetBytes(_content);
            return stream.WriteAsync(bytes, 0, bytes.Length);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
