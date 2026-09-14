using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Http;

public class HttpEndpointQueryStringAndHeadersTests(App app) : AppComponentTest(app)
{
    [Test]
    [Arguments("name=John", "TestAgent/1.0", "Name: John", "UserAgent: TestAgent/1.0")]
    [Arguments("name=Jane", "CustomAgent/2.0", "Name: Jane", "UserAgent: CustomAgent/2.0")]
    [Arguments("name=John&age=30&city=NewYork", "TestAgent/1.0", "Name: John", "UserAgent: TestAgent/1.0")]
    public async Task QueryStringAndHeaders_WithParameters_ReturnsExtractedData(
        string queryString,
        string userAgent,
        string expectedNameFragment,
        string expectedUserAgentFragment)
    {
        // Act
        var response = await GetQueryHeadersResponseAsync(queryString, userAgent);

        // Assert
        await Assert.That(response).Contains(expectedNameFragment);
        await Assert.That(response).Contains(expectedUserAgentFragment);
    }

    [Test]
    public async Task QueryStringAndHeaders_NoParameters_ReturnsDefaultValues()
    {
        // Act
        var response = await GetQueryHeadersResponseAsync();

        // Assert
        await Assert.That(response).Contains("Name: unknown");
        await Assert.That(response).Contains("UserAgent:"); // Should contain UserAgent even if empty
    }

    [Test]
    public async Task QueryStringAndHeaders_UrlEncodedQueryString_ReturnsDecodedValue()
    {
        // Arrange
        var encodedName = Uri.EscapeDataString("John Doe");
        var queryString = $"name={encodedName}";

        // Act
        var response = await GetQueryHeadersResponseAsync(queryString);

        // Assert
        await Assert.That(response).Contains("Name: John Doe");
    }

    [Test]
    public async Task QueryStringAndHeaders_CustomHeaders_ReturnsHeaderValues()
    {
        // Arrange
        var customHeaders = new Dictionary<string, string>
        {
            ["X-Custom-Header"] = "CustomValue",
            ["User-Agent"] = "CustomAgent/2.0"
        };

        // Act
        var response = await GetQueryHeadersResponseAsync("name=Jane", customHeaders: customHeaders);

        // Assert
        await Assert.That(response).Contains("Name: Jane");
        await Assert.That(response).Contains("UserAgent: CustomAgent/2.0");
    }

    private async Task<string> GetQueryHeadersResponseAsync(
        string? queryString = null,
        string? userAgent = null,
        Dictionary<string, string>? customHeaders = null)
    {
        var client = WorkflowServer.CreateHttpWorkflowClient();

        // Build URL
        var url = "test/query-headers";
        if (!string.IsNullOrEmpty(queryString))
        {
            url += $"?{queryString}";
        }

        // Create request
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Add headers to request
        if (!string.IsNullOrEmpty(userAgent))
        {
            request.Headers.Add("User-Agent", userAgent);
        }

        if (customHeaders != null)
        {
            foreach (var header in customHeaders)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }

        using var responseMessage = await client.SendAsync(request);
        responseMessage.EnsureSuccessStatusCode();
        return await responseMessage.Content.ReadAsStringAsync();
    }
}
