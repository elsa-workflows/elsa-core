using System.Net;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Http;

public class HttpEndpointRouteParametersTests(App app) : AppComponentTest(app)
{
    [Theory]
    [InlineData("123", "456", "UserId: 123, OrderId: 456")]
    [InlineData("user-abc", "order-xyz", "UserId: user-abc, OrderId: order-xyz")]
    [InlineData("999", "001", "UserId: 999, OrderId: 001")]
    public async Task RouteParameters_ValidRouteValues_ReturnsExtractedParameters(string userId, string orderId, string expectedContent)
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();

        // Act
        var response = await client.GetStringAsync($"test/users/{userId}/orders/{orderId}");

        // Assert
        Assert.Equal(expectedContent, response);
    }

    [Fact]
    public async Task RouteParameters_UrlEncodedValues_ReturnsDecodedParameters()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        var encodedUserId = Uri.EscapeDataString("user@domain.com");
        var encodedOrderId = Uri.EscapeDataString("order-with-special-chars!");

        // Act
        var response = await client.GetStringAsync($"test/users/{encodedUserId}/orders/{encodedOrderId}");

        // Assert
        Assert.Contains("user@domain.com", response);
        Assert.Contains("order-with-special-chars!", response);
    }

    [Fact]
    public async Task RouteParameters_InvalidRoute_ReturnsNotFound()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();

        // Act
        var response = await client.GetAsync("test/users/123/invalid-path");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RouteParameters_MissingParameter_ReturnsNotFound()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();

        // Act
        var response = await client.GetAsync("test/users/123/orders");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
