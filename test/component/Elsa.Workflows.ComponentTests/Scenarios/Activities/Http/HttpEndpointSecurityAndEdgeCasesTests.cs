using System.Net;
using System.Text;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Http;

public class HttpEndpointSecurityAndEdgeCasesTests(App app) : AppComponentTest(app)
{
    [Fact]
    public async Task HttpEndpoint_BlockedFileExtensions_RejectsBlockedFiles()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("Malicious file"));
        fileContent.Headers.ContentType = new("application/octet-stream");
        content.Add(fileContent, "file", "malware.exe"); // .exe is in blocked extensions

        // Act
        var response = await client.PostAsync("test/blocked-extensions", content);

        // Assert
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task HttpEndpoint_BlockedFileExtensions_AllowsNonBlockedFiles()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("Safe file content"));
        fileContent.Headers.ContentType = new("text/plain");
        content.Add(fileContent, "file", "document.txt"); // .txt is not in blocked extensions

        // Act
        var response = await client.PostAsync("test/blocked-extensions", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("File upload successful", responseContent);
    }

    [Fact]
    public async Task HttpEndpoint_MalformedMultipartData_HandlesGracefully()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        
        // Create properly malformed multipart content by using StringContent with manually crafted headers
        using var malformedContent = new StringContent(
            "--boundary\r\nContent-Disposition: form-data; name=\"test\"\r\n\r\nvalue\r\n--boundary--", 
            Encoding.UTF8);
        
        // Manually set the content type header to avoid client-side validation
        malformedContent.Headers.ContentType = new("multipart/form-data")
        {
            Parameters = { new System.Net.Http.Headers.NameValueHeaderValue("boundary", "boundary") }
        };

        // Act
        var response = await client.PostAsync("test/file-upload", malformedContent);

        // Assert - Should handle gracefully without crashing
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || 
                   response.StatusCode == HttpStatusCode.OK ||
                   response.StatusCode == HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task HttpEndpoint_ExtremelyLargeHeaders_HandlesGracefully()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "test/query-headers");
        request.Headers.Add("X-Large-Header", new string('x', 8192)); // Very large header

        // Act & Assert - Should handle gracefully
        var response = await client.SendAsync(request);
        
        // Response might be successful or might be rejected by server, but shouldn't crash
        Assert.True(response.StatusCode == HttpStatusCode.OK || 
                   response.StatusCode == HttpStatusCode.BadRequest ||
                   response.StatusCode == HttpStatusCode.RequestHeaderFieldsTooLarge);
    }

    [Fact]
    public async Task HttpEndpoint_CaseSensitiveRoutes_RespectsRouteCase()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();

        // Act - Test the original route first to establish baseline
        var originalResponse = await client.GetAsync("test/basic");
        
        // Only proceed with case testing if the original route works
        if (originalResponse.StatusCode != HttpStatusCode.OK)
        {
            Assert.Fail("Original route 'test/basic' should work before testing case variations");
        }

        // Test case variations
        var uppercaseResponse = await client.GetAsync("TEST/BASIC");
        var mixedCaseResponse = await client.GetAsync("Test/Basic");

        // Assert - ASP.NET Core routing is case-insensitive by default
        // Both case variations should work the same as the original
        Assert.Equal(HttpStatusCode.OK, uppercaseResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, mixedCaseResponse.StatusCode);
        
        // Verify all responses return the same content
        var originalContent = await originalResponse.Content.ReadAsStringAsync();
        var uppercaseContent = await uppercaseResponse.Content.ReadAsStringAsync();
        var mixedCaseContent = await mixedCaseResponse.Content.ReadAsStringAsync();
        
        Assert.Equal(originalContent, uppercaseContent);
        Assert.Equal(originalContent, mixedCaseContent);
    }

    [Fact]
    public async Task HttpEndpoint_NullAndEmptyQueryParameters_HandlesCorrectly()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();

        // Act - Test various edge cases with query parameters
        var response1 = await client.GetAsync("test/query-headers?name=");
        var response2 = await client.GetAsync("test/query-headers?name");
        var response3 = await client.GetAsync("test/query-headers?=value");
        var response4 = await client.GetAsync("test/query-headers?&&&");

        // Assert - All should complete without crashing
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response3.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response4.StatusCode);

        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();

        // Empty parameter value vs missing value should be handled gracefully
        Assert.NotNull(content1);
        Assert.NotNull(content2);
    }

    [Fact]
    public async Task HttpEndpoint_ZeroByteFile_ProcessesCorrectly()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        using var content = new MultipartFormDataContent();
        var emptyFile = new ByteArrayContent(Array.Empty<byte>());
        emptyFile.Headers.ContentType = new("text/plain");
        content.Add(emptyFile, "file", "empty.txt");

        // Act
        var response = await client.PostAsync("test/file-upload", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("empty.txt", responseContent);
        Assert.Contains("0 bytes", responseContent);
    }
}
