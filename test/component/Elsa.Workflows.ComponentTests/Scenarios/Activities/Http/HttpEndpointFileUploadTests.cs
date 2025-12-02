using System.Net;
using System.Text;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Http;

public class HttpEndpointFileUploadTests(App app) : AppComponentTest(app)
{
    [Fact]
    public async Task FileUpload_SingleFile_ReturnsFileDetails()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("Test file content"));
        fileContent.Headers.ContentType = new("text/plain");
        content.Add(fileContent, "file", "test.txt");

        // Act
        var response = await client.PostAsync("test/file-upload", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("test.txt", responseContent);
        Assert.Contains("17 bytes", responseContent); // "Test file content" is 17 bytes
        Assert.Contains("text/plain", responseContent);
    }

    [Fact]
    public async Task FileUpload_MultipleFiles_ReturnsAllFileDetails()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        var content = new MultipartFormDataContent();
        
        var file1Content = new ByteArrayContent(Encoding.UTF8.GetBytes("File 1 content"));
        file1Content.Headers.ContentType = new("text/plain");
        content.Add(file1Content, "file1", "file1.txt");
        
        var file2Content = new ByteArrayContent(Encoding.UTF8.GetBytes("File 2 content"));
        file2Content.Headers.ContentType = new("text/plain");
        content.Add(file2Content, "file2", "file2.txt");

        // Act
        var response = await client.PostAsync("test/file-upload", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("file1.txt", responseContent);
        Assert.Contains("file2.txt", responseContent);
        Assert.Contains("14 bytes", responseContent); // Each file is 14 bytes
    }

    [Fact]
    public async Task FileUpload_NoFiles_ReturnsNoFilesMessage()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("value"), "field");

        // Act
        var response = await client.PostAsync("test/file-upload", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("No files uploaded", responseContent);
    }

    [Fact]
    public async Task FileUpload_EmptyFile_ReturnsZeroBytesFile()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([]);
        fileContent.Headers.ContentType = new("text/plain");
        content.Add(fileContent, "file", "empty.txt");

        // Act
        var response = await client.PostAsync("test/file-upload", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("empty.txt", responseContent);
        Assert.Contains("0 bytes", responseContent);
    }

    [Fact]
    public async Task FileUpload_WithFormFields_ProcessesBothFilesAndFields()
    {
        // Arrange
        var client = WorkflowServer.CreateHttpWorkflowClient();
        var content = new MultipartFormDataContent();
        
        // Add file
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("Test content"));
        fileContent.Headers.ContentType = new("text/plain");
        content.Add(fileContent, "file", "test.txt");
        
        // Add form field
        content.Add(new StringContent("John Doe"), "name");

        // Act
        var response = await client.PostAsync("test/file-upload", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("test.txt", responseContent);
        Assert.Contains("12 bytes", responseContent); // "Test content" is 12 bytes
    }
}

