using System.Net;
using System.Text;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Http;

public class HttpEndpointTests(App app) : AppComponentTest(app)
{

    [Fact]
    public async Task BasicHttpEndpoint_Get_ReturnsExpectedResponse()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();

        // Act
        var response = await client.GetStringAsync("test/basic");

        // Assert
        Assert.Equal("Basic HttpEndpoint Test Response", response);     
    }

    [Fact]
    public async Task BasicHttpEndpoint_UnsupportedMethod_ReturnsNotFound()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();

        // Act
        var response = await client.PostAsync("test/basic", new StringContent("", Encoding.UTF8, "text/plain"));

        // Assert
        // In this test environment, unsupported methods on unregistered endpoints return NotFound
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task MultipleHttpMethods_SupportedMethods_ReturnsMethodName(string method)
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        var request = new HttpRequestMessage(new HttpMethod(method), "test/multi-method");

        // Act
        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal($"Method: {method}", content);
    }

    [Fact]
    public async Task MultipleHttpMethods_UnsupportedMethod_ReturnsNotFound()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        var request = new HttpRequestMessage(HttpMethod.Patch, "test/multi-method");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        // In this test environment, unsupported methods on unregistered endpoints return NotFound
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task HttpEndpoint_WorkflowCompletesCleanlySynchronously()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();

        // Act - Make HTTP request to trigger the workflow
        var response = await client.GetAsync("test/basic");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert - Verify the workflow completed and returned the expected response
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Basic HttpEndpoint Test Response", responseContent);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        
        // The fact that we get a response means the workflow completed synchronously
        // without hanging or requiring additional triggers
    }


    [Fact]
    public async Task HttpEndpoint_ConcurrentRequests_ProcessesAllSuccessfully()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        var tasks = new List<Task<string>>();

        // Act - Send 10 concurrent requests
        for (var i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(client.GetStringAsync($"test/users/user-{index}/orders/order-{index}"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, responses.Length);
        for (int i = 0; i < 10; i++)
        {
            Assert.Contains($"UserId: user-{i}", responses[i]);
            Assert.Contains($"OrderId: order-{i}", responses[i]);
        }
    }

    [Fact]
    public async Task HttpEndpoint_SpecialCharactersInRoute_HandlesCorrectly()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        var specialUserId = Uri.EscapeDataString("user@domain.com");
        var specialOrderId = Uri.EscapeDataString("order-with-special-chars!@#$%");

        // Act
        var response = await client.GetStringAsync($"test/users/{specialUserId}/orders/{specialOrderId}");

        // Assert
        Assert.Contains("user@domain.com", response);
        Assert.Contains("order-with-special-chars!@#$%", response);
    }
    
}
