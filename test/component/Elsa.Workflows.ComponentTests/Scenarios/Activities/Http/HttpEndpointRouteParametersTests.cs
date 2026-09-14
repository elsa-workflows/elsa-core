using System.Net;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Http;

public class HttpEndpointRouteParametersTests(App app) : AppComponentTest(app)
{
    [Test]
    [Arguments("123", "456", "UserId: 123, OrderId: 456")]
    [Arguments("user-abc", "order-xyz", "UserId: user-abc, OrderId: order-xyz")]
    [Arguments("999", "001", "UserId: 999, OrderId: 001")]
    public async Task RouteParameters_ValidRouteValues_ReturnsExtractedParameters(string userId, string orderId, string expectedContent)
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();

        // Act
        var response = await client.GetStringAsync($"test/users/{userId}/orders/{orderId}");

        // Assert
        await Assert.That(response).IsEqualTo(expectedContent);
    }

    [Test]
    public async Task RouteParameters_UrlEncodedValues_ReturnsDecodedParameters()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        var encodedUserId = Uri.EscapeDataString("user@domain.com");
        var encodedOrderId = Uri.EscapeDataString("order-with-special-chars!");

        // Act
        var response = await client.GetStringAsync($"test/users/{encodedUserId}/orders/{encodedOrderId}");

        // Assert
        await Assert.That(response).Contains("user@domain.com");
        await Assert.That(response).Contains("order-with-special-chars!");
    }

    [Test]
    public async Task RouteParameters_InvalidRoute_ReturnsNotFound()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();

        // Act
        using var response = await client.GetAsync("test/users/123/invalid-path");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task RouteParameters_MissingParameter_ReturnsNotFound()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();

        // Act
        using var response = await client.GetAsync("test/users/123/orders");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }
}
