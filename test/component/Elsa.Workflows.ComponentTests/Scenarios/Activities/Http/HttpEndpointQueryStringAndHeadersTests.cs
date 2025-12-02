using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Http;

public class HttpEndpointQueryStringAndHeadersTests(App app) : AppComponentTest(app)
{
    [Fact]
    public async Task QueryStringAndHeaders_WithParameters_ReturnsExtractedData()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        client.DefaultRequestHeaders.Add("User-Agent", "TestAgent/1.0");

        // Act
        var response = await client.GetStringAsync("test/query-headers?name=John");

        // Assert
        Assert.Contains("Name: John", response);
        Assert.Contains("UserAgent: TestAgent/1.0", response);
    }

    [Fact]
    public async Task QueryStringAndHeaders_NoParameters_ReturnsDefaultValues()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();

        // Act
        var response = await client.GetStringAsync("test/query-headers");

        // Assert
        Assert.Contains("Name: unknown", response);
        Assert.Contains("UserAgent:", response); // Should contain UserAgent even if empty
    }

    [Fact]
    public async Task QueryStringAndHeaders_MultipleQueryParameters_ReturnsFirstParameterValue()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();

        // Act
        var response = await client.GetStringAsync("test/query-headers?name=John&age=30&city=NewYork");

        // Assert
        Assert.Contains("Name: John", response);
    }

    [Fact]
    public async Task QueryStringAndHeaders_UrlEncodedQueryString_ReturnsDecodedValue()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        var encodedName = Uri.EscapeDataString("John Doe");

        // Act
        var response = await client.GetStringAsync($"test/query-headers?name={encodedName}");

        // Assert
        Assert.Contains("Name: John Doe", response);
    }

    [Fact]
    public async Task QueryStringAndHeaders_CustomHeaders_ReturnsHeaderValues()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        client.DefaultRequestHeaders.Add("X-Custom-Header", "CustomValue");
        client.DefaultRequestHeaders.Add("User-Agent", "CustomAgent/2.0");

        // Act
        var response = await client.GetStringAsync("test/query-headers?name=Jane");

        // Assert
        Assert.Contains("Name: Jane", response);
        Assert.Contains("UserAgent: CustomAgent/2.0", response);
    }
}
