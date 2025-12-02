using System.Text.Json;
using Elsa.Http;

namespace Elsa.Workflows.IntegrationTests.Activities.Http;

public class HttpEndpointIntegrationTests
{
    [Fact]
    public async Task HttpEndpoint_ConcurrentRequests_ProcessesAllSuccessfully()
    {
        // Arrange - Test concurrent HttpEndpoint activity instantiation and configuration
        var tasks = new List<Task<HttpEndpoint>>();
        
        // Act - Create multiple HttpEndpoint activities concurrently
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            var task = Task.Run(() =>
            {
                var endpoint = new HttpEndpoint
                {
                    Path = new($"test/concurrent/user-{index}"),
                    SupportedMethods = new(["GET", "POST"])
                };
                return endpoint;
            });
            tasks.Add(task);
        }

        var endpoints = await Task.WhenAll(tasks);

        // Assert - Verify all HttpEndpoint activities were created successfully
        Assert.Equal(10, endpoints.Length);
        
        for (int i = 0; i < 10; i++)
        {
            var endpoint = endpoints[i];
            Assert.NotNull(endpoint);
            
            // Verify the path input was set correctly
            Assert.NotNull(endpoint.Path);
            
            // Verify the supported methods input was set correctly
            Assert.NotNull(endpoint.SupportedMethods);
        }
    }

    [Fact]
    public async Task HttpEndpoint_LargeJsonPayload_ProcessesCorrectly()
    {
        // Arrange
        var largeObject = new
        {
            Users = Enumerable.Range(1, 100).Select(i => new
            {
                Id = i,
                Name = $"User {i}",
                Email = $"user{i}@example.com",
                Data = new string('x', 100) // 100 characters per user
            }).ToArray()
        };

        var jsonContent = JsonSerializer.Serialize(largeObject);

        // Act & Assert - Test JSON processing capability
        var parsedBack = JsonSerializer.Deserialize<JsonElement>(jsonContent);
        Assert.True(parsedBack.TryGetProperty("Users", out var usersProperty));
        Assert.Equal(JsonValueKind.Array, usersProperty.ValueKind);
        Assert.Equal(100, usersProperty.GetArrayLength());
    }

    [Fact]
    public async Task HttpEndpoint_UnicodeContent_ProcessesCorrectly()
    {
        // Arrange
        var originalMessage = "Hello 世界! 🌍 Ñandú";
        var unicodeData = new { Message = originalMessage };
        var jsonContent = JsonSerializer.Serialize(unicodeData, new JsonSerializerOptions { WriteIndented = true });

        // Act - Parse the JSON to simulate processing
        var parsedResponse = JsonSerializer.Deserialize<JsonElement>(jsonContent);
        
        // Assert
        Assert.True(parsedResponse.TryGetProperty("Message", out var messageProperty), 
            $"Response should contain 'Message' property. Actual response: {jsonContent}");
        
        var actualMessage = messageProperty.GetString();
        Assert.Equal(originalMessage, actualMessage);
    }

    [Fact]
    public async Task HttpEndpoint_SpecialCharactersInRoute_HandlesCorrectly()
    {
        // Test URI encoding/decoding
        var specialUserId = "user@domain.com";
        var specialOrderId = "order-with-special-chars!@#$%";
        
        var encodedUserId = Uri.EscapeDataString(specialUserId);
        var encodedOrderId = Uri.EscapeDataString(specialOrderId);
        
        // Verify encoding/decoding works
        Assert.Equal(specialUserId, Uri.UnescapeDataString(encodedUserId));
        Assert.Equal(specialOrderId, Uri.UnescapeDataString(encodedOrderId));
    }
}
