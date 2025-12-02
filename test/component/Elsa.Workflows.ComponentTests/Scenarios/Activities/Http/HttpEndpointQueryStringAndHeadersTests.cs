using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Http;

public class HttpEndpointQueryStringAndHeadersTests(App app) : AppComponentTest(app)
{
    [Theory]
    [InlineData("name=John", "TestAgent/1.0", "Name: John", "UserAgent: TestAgent/1.0")]
    [InlineData("name=Jane", "CustomAgent/2.0", "Name: Jane", "UserAgent: CustomAgent/2.0")]
    [InlineData("name=John&age=30&city=NewYork", "TestAgent/1.0", "Name: John", "UserAgent: TestAgent/1.0")]
    public async Task QueryStringAndHeaders_WithParameters_ReturnsExtractedData(
        string queryString,
        string userAgent,
        string expectedNameFragment,
        string expectedUserAgentFragment)
    {
        // Act
        var response = await GetQueryHeadersResponseAsync(queryString, userAgent);

        // Assert
        Assert.Contains(expectedNameFragment, response);
        Assert.Contains(expectedUserAgentFragment, response);
    }

    [Fact]
    public async Task QueryStringAndHeaders_NoParameters_ReturnsDefaultValues()
    {
        // Act
        var response = await GetQueryHeadersResponseAsync();

        // Assert
        Assert.Contains("Name: unknown", response);
        Assert.Contains("UserAgent:", response); // Should contain UserAgent even if empty
    }

    [Fact]
    public async Task QueryStringAndHeaders_UrlEncodedQueryString_ReturnsDecodedValue()
    {
        // Arrange
        var encodedName = Uri.EscapeDataString("John Doe");
        var queryString = $"name={encodedName}";

        // Act
        var response = await GetQueryHeadersResponseAsync(queryString);

        // Assert
        Assert.Contains("Name: John Doe", response);
    }

    [Fact]
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
        Assert.Contains("Name: Jane", response);
        Assert.Contains("UserAgent: CustomAgent/2.0", response);
    }

    private async Task<string> GetQueryHeadersResponseAsync(
        string? queryString = null,
        string? userAgent = null,
        Dictionary<string, string>? customHeaders = null)
    {
        var client = WorkflowServer.CreateHttpWorkflowClient();

        // Add headers
        if (!string.IsNullOrEmpty(userAgent))
        {
            client.DefaultRequestHeaders.Add("User-Agent", userAgent);
        }

        if (customHeaders != null)
        {
            foreach (var header in customHeaders)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        // Build URL
        var url = "test/query-headers";
        if (!string.IsNullOrEmpty(queryString))
        {
            url += $"?{queryString}";
        }

        return await client.GetStringAsync(url);
    }
}
