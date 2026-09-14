using System.Net;
using System.Text;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Http;

public class HttpEndpointSecurityAndEdgeCasesTests(App app) : AppComponentTest(app)
{
    [Test]
    public async Task HttpEndpoint_BlockedFileExtensions_RejectsBlockedFiles()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("Malicious file"));
        fileContent.Headers.ContentType = new("application/octet-stream");
        content.Add(fileContent, "file", "malware.exe"); // .exe is in blocked extensions

        // Act
        using var response = await client.PostAsync("test/blocked-extensions", content);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.UnsupportedMediaType);
    }

    [Test]
    public async Task HttpEndpoint_BlockedFileExtensions_AllowsNonBlockedFiles()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("Safe file content"));
        fileContent.Headers.ContentType = new("text/plain");
        content.Add(fileContent, "file", "document.txt"); // .txt is not in blocked extensions

        // Act
        using var response = await client.PostAsync("test/blocked-extensions", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(responseContent).IsEqualTo("File upload successful");
    }
    
    [Test]
    [Arguments("test/basic")]
    [Arguments("TEST/BASIC")]
    [Arguments("Test/Basic")]
    [Arguments("test/BASIC")]
    [Arguments("TEST/basic")]
    public async Task HttpEndpoint_CaseSensitiveRoutes_RespectsRouteCase(string route)
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();

        // Act
        using var response = await client.GetAsync(route);
        var content = await response.Content.ReadAsStringAsync();

        // Assert - ASP.NET Core routing is case-insensitive by default
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(content).IsNotNull();
    }

    [Test]
    [Arguments("test/query-headers?name=")]
    [Arguments("test/query-headers?name")]
    [Arguments("test/query-headers?=value")]
    [Arguments("test/query-headers?&&&")]
    public async Task HttpEndpoint_NullAndEmptyQueryParameters_HandlesCorrectly(string url)
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();

        // Act
        using var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Should handle edge cases with query parameters gracefully
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(content).IsNotNull();
    }

    [Test]
    public async Task HttpEndpoint_ZeroByteFile_ProcessesCorrectly()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        using var content = new MultipartFormDataContent();
        var emptyFile = new ByteArrayContent(Array.Empty<byte>());
        emptyFile.Headers.ContentType = new("text/plain");
        content.Add(emptyFile, "file", "empty.txt");

        // Act
        using var response = await client.PostAsync("test/file-upload", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(responseContent).Contains("empty.txt");
        await Assert.That(responseContent).Contains("0 bytes");
    }
}
