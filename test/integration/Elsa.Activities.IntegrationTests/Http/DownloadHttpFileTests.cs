using System.Net;
using Elsa.Extensions;
using Elsa.Http;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Activities.IntegrationTests.Http;

public class DownloadHttpFileTests(ITestOutputHelper testOutputHelper)
{
    [Fact(DisplayName = "DownloadHttpFile downloads file successfully")]
    public async Task DownloadsFile_Successfully()
    {
        // Arrange
        var fileContent = "Test file content"u8.ToArray();
        var handler = CreateFileResponseHandler(fileContent, "document.pdf", "application/pdf");

        // Act
        var (workflowResult, activity) = await RunActivityAsync("https://example.com/file.pdf", handler);

        // Assert
        var file = workflowResult.GetActivityOutput<HttpFile>(activity);
        Assert.NotNull(file);
        Assert.Equal("document.pdf", file.Filename);
        Assert.Equal("application/pdf", file.ContentType);

        var stream = workflowResult.GetActivityOutput<Stream>(activity, nameof(DownloadHttpFile.ResponseContentStream));
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        Assert.Equal("Test file content", content);
    }

    [Fact(DisplayName = "DownloadHttpFile handles POST requests")]
    public async Task HandlesPostRequest()
    {
        // Arrange
        var fileContent = "Response content"u8.ToArray();
        var handler = CreateFileResponseHandler(fileContent);

        // Act
        var (workflowResult, activity) = await RunActivityAsync(
            "https://api.example.com/download",
            handler,
            method: "POST",
            requestContent: new { id = 123 },
            requestContentType: "application/json");

        // Assert
        var statusCode = workflowResult.GetActivityOutput<int>(activity, nameof(DownloadHttpFile.StatusCode));
        Assert.Equal(200, statusCode);

        var file = workflowResult.GetActivityOutput<HttpFile>(activity);
        Assert.NotNull(file);
    }

    [Fact(DisplayName = "DownloadHttpFile includes authorization header")]
    public async Task IncludesAuthorizationHeader()
    {
        // Arrange
        var requestCapture = new HttpRequestMessage?[1];
        var fileContent = "Authorized content"u8.ToArray();
        var handler = CreateCapturingHandler(fileContent, requestCapture);

        // Act
        await RunActivityAsync(
            "https://example.com/secure/file.pdf",
            handler,
            authorization: "Bearer test-token");

        // Assert
        var capturedRequest = requestCapture[0];
        Assert.NotNull(capturedRequest);
        Assert.NotNull(capturedRequest.Headers.Authorization);
        Assert.Equal("Bearer test-token", capturedRequest.Headers.Authorization.ToString());
    }

    [Theory(DisplayName = "DownloadHttpFile extracts filename correctly")]
    [InlineData("https://example.com/generate-report", "annual-report.xlsx", "annual-report.xlsx")] // From Content-Disposition
    [InlineData("https://example.com/downloads/image.png", null, "image.png")] // From URL
    public async Task ExtractsFilename(string url, string? contentDispositionFilename, string expectedFilename)
    {
        // Arrange
        var fileContent = "File data"u8.ToArray();
        var handler = CreateFileResponseHandler(fileContent, contentDispositionFilename);

        // Act
        var (workflowResult, activity) = await RunActivityAsync(url, handler);

        // Assert
        var file = workflowResult.GetActivityOutput<HttpFile>(activity);
        Assert.NotNull(file);
        Assert.Equal(expectedFilename, file.Filename);
    }

    [Theory(DisplayName = "DownloadHttpFile handles various status codes")]
    [InlineData(200)]
    [InlineData(201)]
    [InlineData(404)]
    public async Task HandlesStatusCodes(int statusCode)
    {
        // Arrange
        var handler = CreateFileResponseHandler(statusCode: (HttpStatusCode)statusCode);

        // Act
        var (workflowResult, activity) = await RunActivityAsync(
            "https://example.com/file.pdf",
            handler,
            expectedStatusCodes: [statusCode]);

        // Assert
        var actualStatusCode = workflowResult.GetActivityOutput<int>(activity, nameof(DownloadHttpFile.StatusCode));
        Assert.Equal(statusCode, actualStatusCode);
    }

    [Fact(DisplayName = "DownloadHttpFile sets response headers")]
    public async Task SetsResponseHeaders()
    {
        // Arrange
        var additionalHeaders = new Dictionary<string, string>
        {
            { "X-Rate-Limit", "100" },
            { "X-Request-Id", "abc123" }
        };
        var handler = CreateFileResponseHandler(additionalHeaders: additionalHeaders);

        // Act
        var (workflowResult, activity) = await RunActivityAsync("https://api.example.com/download", handler);

        // Assert
        var responseHeaders = workflowResult.GetActivityOutput<HttpHeaders>(activity, nameof(DownloadHttpFile.ResponseHeaders));
        Assert.NotNull(responseHeaders);
        Assert.Contains(responseHeaders.Keys, k => k.Equals("X-Rate-Limit", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(responseHeaders.Keys, k => k.Equals("X-Request-Id", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<(RunWorkflowResult Result, DownloadHttpFile Activity)> RunActivityAsync(
        string url,
        HttpMessageHandler handler,
        string method = "GET",
        object? requestContent = null,
        string? requestContentType = null,
        string? authorization = null,
        int[]? expectedStatusCodes = null)
    {
        var fixture = CreateFixture(handler);
        var activity = new DownloadHttpFile
        {
            Url = new(new Uri(url)),
            Method = new(method),
            ExpectedStatusCodes = new(expectedStatusCodes ?? [200])
        };

        if (requestContent != null)
            activity.RequestContent = new(requestContent);

        if (requestContentType != null)
            activity.RequestContentType = new(requestContentType);

        if (authorization != null)
            activity.Authorization = new(authorization);

        var result = await fixture.RunActivityAsync(activity);
        return (result, activity);
    }

    private WorkflowTestFixture CreateFixture(HttpMessageHandler handler) =>
        new WorkflowTestFixture(testOutputHelper)
            .ConfigureElsa(elsa => elsa.UseHttp(http =>
            {
                http.HttpClientBuilder = builder => builder.ConfigurePrimaryHttpMessageHandler(() => handler);
            }));

    private static HttpMessageHandler CreateFileResponseHandler(
        byte[]? content = null,
        string? filename = null,
        string? contentType = null,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        Dictionary<string, string>? additionalHeaders = null) =>
        new TestHttpMessageHandler((_, _) =>
        {
            // Response ownership is transferred to the caller (HttpClient), which will dispose it
            var response = new HttpResponseMessage(statusCode);

            // Always set Content (even if empty) because DownloadHttpFile accesses Content.Headers
            var actualContent = content ?? (statusCode == HttpStatusCode.OK ? "Default file content"u8.ToArray() : []);
            var actualContentType = contentType ?? "application/octet-stream";
            response.Content = new ByteArrayContent(actualContent);
            response.Content.Headers.ContentType = new(actualContentType);

            if (filename != null)
            {
                response.Content.Headers.ContentDisposition = new("attachment")
                {
                    FileName = filename
                };
            }

            if (additionalHeaders != null)
                foreach (var header in additionalHeaders)
                    response.Headers.Add(header.Key, header.Value);

            return Task.FromResult(response);
        });

    private static HttpMessageHandler CreateCapturingHandler(byte[] content, HttpRequestMessage?[] capture) =>
        new TestHttpMessageHandler((request, _) =>
        {
            capture[0] = request;

            // Response ownership is transferred to the caller (HttpClient), which will dispose it
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(content)
            };
            response.Content.Headers.ContentType = new("application/octet-stream");

            return Task.FromResult(response);
        });

    private sealed class TestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            handler(request, cancellationToken);
    }
}
