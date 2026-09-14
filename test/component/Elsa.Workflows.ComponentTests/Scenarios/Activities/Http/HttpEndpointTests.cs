using System.Net;
using System.Text;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Http;

public class HttpEndpointTests(App app) : AppComponentTest(app)
{
    private const int ConcurrentRequestCount = 3;

    [Test]
    public async Task BasicHttpEndpoint_UnsupportedMethod_ReturnsNotFound()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();

        // Act
        using var content = new StringContent("", Encoding.UTF8, "text/plain");
        using var response = await client.PostAsync("test/basic", content);

        // Assert
        // In this test environment, unsupported methods on unregistered endpoints return NotFound
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    [Arguments("GET")]
    [Arguments("POST")]
    [Arguments("PUT")]
    [Arguments("DELETE")]
    public async Task MultipleHttpMethods_SupportedMethods_ReturnsMethodName(string method)
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), "test/multi-method");

        // Act
        using var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(content).IsEqualTo($"Method: {method}");
    }

    [Test]
    public async Task MultipleHttpMethods_UnsupportedMethod_ReturnsNotFound()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        using var request = new HttpRequestMessage(HttpMethod.Patch, "test/multi-method");
        // Act
        using var response = await client.SendAsync(request);

        // Assert
        // In this test environment, unsupported methods on unregistered endpoints return NotFound
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task HttpEndpoint_WorkflowCompletesCleanlySynchronously()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();

        // Act - Make HTTP request to trigger the workflow
        using var response = await client.GetAsync("test/basic");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert - Verify the workflow completed and returned the expected response
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(responseContent).IsEqualTo("Basic HttpEndpoint Test Response");
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("text/plain");

        // The fact that we get a response means the workflow completed synchronously
        // without hanging or requiring additional triggers
    }
    
    [Test]
    public async Task HttpEndpoint_ConcurrentRequests_ProcessesAllSuccessfully()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var tasks = new List<Task<string>>();

        // Act - Send a small concurrent batch. Larger batches can deadlock SQL Server
        // while persisting activity execution logs, which is not what this test covers.
        for (var i = 0; i < ConcurrentRequestCount; i++)
        {
            var index = i;
            tasks.Add(client.GetStringAsync($"test/users/user-{index}/orders/order-{index}", cancellationTokenSource.Token));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        await Assert.That(responses.Length).IsEqualTo(ConcurrentRequestCount);
        for (var i = 0; i < ConcurrentRequestCount; i++)
        {
            await Assert.That(responses[i]).Contains($"UserId: user-{i}");
            await Assert.That(responses[i]).Contains($"OrderId: order-{i}");
        }
    }

    [Test]
    public async Task HttpEndpoint_SpecialCharactersInRoute_HandlesCorrectly()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        var specialUserId = Uri.EscapeDataString("user@domain.com");
        var specialOrderId = Uri.EscapeDataString("order-with-special-chars!@#$%");

        // Act
        var response = await client.GetStringAsync($"test/users/{specialUserId}/orders/{specialOrderId}");

        // Assert
        await Assert.That(response).Contains("user@domain.com");
        await Assert.That(response).Contains("order-with-special-chars!@#$%");
    }
    
}
