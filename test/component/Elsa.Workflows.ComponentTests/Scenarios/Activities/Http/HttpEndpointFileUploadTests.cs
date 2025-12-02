using System.Net;
using System.Text;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Http;

public class HttpEndpointFileUploadTests(App app) : AppComponentTest(app)
{
    [Theory]
    [InlineData("Test file content", "test.txt", "text/plain", "17 bytes")]
    [InlineData("Sample document", "sample.txt", "text/plain", "15 bytes")]
    [InlineData("", "empty.txt", "text/plain", "0 bytes")]
    public async Task FileUpload_SingleFile_ReturnsExpectedMetadata(
        string fileContent,
        string fileName,
        string contentType,
        string expectedSizeText)
    {
        // Arrange
        var fileData = Encoding.UTF8.GetBytes(fileContent);

        // Act
        var response = await PostSingleFileAsync(fileData, fileName, contentType);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        AssertOkResponseContains(response, responseContent, fileName, expectedSizeText, contentType);
    }

    [Fact]
    public async Task FileUpload_MultipleFiles_ReturnsAllFileDetails()
    {
        // Arrange
        var files = new[]
        {
            ("File 1 content", "file1.txt", "text/plain"),
            ("File 2 content", "file2.txt", "text/plain")
        };

        // Act
        var response = await PostMultipleFilesAsync(files);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        AssertOkResponseContains(response, responseContent, "file1.txt", "file2.txt", "14 bytes");
    }

    [Fact]
    public async Task FileUpload_NoFiles_ReturnsNoFilesMessage()
    {
        // Act
        var response = await PostFormDataWithoutFilesAsync();
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        AssertOkResponseContains(response, responseContent, "No files uploaded");
    }

    [Fact]
    public async Task FileUpload_WithFormFields_ProcessesBothFilesAndFields()
    {
        // Arrange
        var fileData = Encoding.UTF8.GetBytes("Test content");
        var formFields = new[] { ("name", "John Doe") };

        // Act
        var response = await PostFileWithFormFieldsAsync(fileData, "test.txt", "text/plain", formFields);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        AssertOkResponseContains(response, responseContent, "test.txt", "12 bytes");
    }

    private async Task<HttpResponseMessage> PostSingleFileAsync(
        byte[] fileData, 
        string fileName, 
        string contentType)
    {
        var client = WorkflowServer.CreateHttpWorkflowClient();
        using var content = new MultipartFormDataContent();
        
        var fileContent = new ByteArrayContent(fileData);
        fileContent.Headers.ContentType = new(contentType);
        content.Add(fileContent, "file", fileName);

        return await client.PostAsync("test/file-upload", content);
    }

    private async Task<HttpResponseMessage> PostMultipleFilesAsync(
        (string content, string fileName, string contentType)[] files)
    {
        var client = WorkflowServer.CreateHttpWorkflowClient();
        using var content = new MultipartFormDataContent();

        for (int i = 0; i < files.Length; i++)
        {
            var file = files[i];
            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(file.content));
            fileContent.Headers.ContentType = new(file.contentType);
            content.Add(fileContent, $"file{i + 1}", file.fileName);
        }

        return await client.PostAsync("test/file-upload", content);
    }

    private async Task<HttpResponseMessage> PostFormDataWithoutFilesAsync()
    {
        var client = WorkflowServer.CreateHttpWorkflowClient();
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("value"), "field");

        return await client.PostAsync("test/file-upload", content);
    }

    private async Task<HttpResponseMessage> PostFileWithFormFieldsAsync(
        byte[] fileData, 
        string fileName, 
        string contentType, 
        (string name, string value)[] formFields)
    {
        var client = WorkflowServer.CreateHttpWorkflowClient();
        using var content = new MultipartFormDataContent();

        // Add file
        var fileContent = new ByteArrayContent(fileData);
        fileContent.Headers.ContentType = new(contentType);
        content.Add(fileContent, "file", fileName);

        // Add form fields
        foreach (var (name, value) in formFields)
        {
            content.Add(new StringContent(value), name);
        }

        return await client.PostAsync("test/file-upload", content);
    }

    private static void AssertOkResponseContains(
        HttpResponseMessage response, 
        string responseContent, 
        params string[] expectedFragments)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        foreach (var fragment in expectedFragments)
        {
            Assert.Contains(fragment, responseContent);
        }
    }
}

