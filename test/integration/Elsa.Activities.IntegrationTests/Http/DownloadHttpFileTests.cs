using System.Net;
using Elsa.Extensions;
using Elsa.Http;
using Elsa.Testing.Shared;
using Elsa.Workflows;
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
        var fixture = CreateFixture(handler);
        var activity = new DownloadHttpFile
        {
            Url = new(new Uri("https://example.com/file.pdf")),
            Method = new("GET"),
            ExpectedStatusCodes = new([200])
        };

        // Act
        var result = await fixture.RunActivityAsync(activity);

        // Assert
        var file = result.GetActivityOutput<HttpFile>(activity);
        Assert.NotNull(file);
        Assert.Equal("document.pdf", file.Filename);
        Assert.Equal("application/pdf", file.ContentType);

        var stream = result.GetActivityOutput<Stream>(activity, nameof(DownloadHttpFile.ResponseContentStream));
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
        var fixture = CreateFixture(handler);
        var activity = new DownloadHttpFile
        {
            Url = new(new Uri("https://api.example.com/download")),
            Method = new("POST"),
            RequestContent = new(new { id = 123 }),
            RequestContentType = new("application/json"),
            ExpectedStatusCodes = new([200])
        };

        // Act
        var result = await fixture.RunActivityAsync(activity);

        // Assert
        var statusCode = result.GetActivityOutput<int>(activity, nameof(DownloadHttpFile.StatusCode));
        Assert.Equal(200, statusCode);

        var file = result.GetActivityOutput<HttpFile>(activity);
        Assert.NotNull(file);
    }

    [Fact(DisplayName = "DownloadHttpFile includes authorization header")]
    public async Task IncludesAuthorizationHeader()
    {
        // Arrange
        var requestCapture = new HttpRequestMessage?[1];
        var fileContent = "Authorized content"u8.ToArray();
        var handler = CreateCapturingHandler(fileContent, requestCapture);
        var fixture = CreateFixture(handler);
        var activity = new DownloadHttpFile
        {
            Url = new(new Uri("https://example.com/secure/file.pdf")),
            Method = new("GET"),
            Authorization = new("Bearer test-token"),
            ExpectedStatusCodes = new([200])
        };

        // Act
        await fixture.RunActivityAsync(activity);

        // Assert
        var capturedRequest = requestCapture[0];
        Assert.NotNull(capturedRequest);
        Assert.NotNull(capturedRequest.Headers.Authorization);
        Assert.Equal("Bearer test-token", capturedRequest.Headers.Authorization.ToString());
    }

    [Fact(DisplayName = "DownloadHttpFile extracts filename from Content-Disposition")]
    public async Task ExtractsFilename_FromContentDisposition()
    {
        // Arrange
        var fileContent = "Report data"u8.ToArray();
        var handler = CreateFileResponseHandler(fileContent, "annual-report.xlsx");
        var fixture = CreateFixture(handler);
        var activity = new DownloadHttpFile
        {
            Url = new(new Uri("https://example.com/generate-report")),
            Method = new("GET"),
            ExpectedStatusCodes = new([200])
        };

        // Act
        var result = await fixture.RunActivityAsync(activity);

        // Assert
        var file = result.GetActivityOutput<HttpFile>(activity);
        Assert.NotNull(file);
        Assert.Equal("annual-report.xlsx", file.Filename);
    }

    [Fact(DisplayName = "DownloadHttpFile extracts filename from URL when no Content-Disposition")]
    public async Task ExtractsFilename_FromUrl()
    {
        // Arrange
        var fileContent = "File data"u8.ToArray();
        var handler = CreateFileResponseHandler(fileContent, filename: null);
        var fixture = CreateFixture(handler);
        var activity = new DownloadHttpFile
        {
            Url = new(new Uri("https://example.com/downloads/image.png")),
            Method = new("GET"),
            ExpectedStatusCodes = new([200])
        };

        // Act
        var result = await fixture.RunActivityAsync(activity);

        // Assert
        var file = result.GetActivityOutput<HttpFile>(activity);
        Assert.NotNull(file);
        Assert.Equal("image.png", file.Filename);
    }

    [Theory(DisplayName = "DownloadHttpFile handles various status codes")]
    [InlineData(200)]
    [InlineData(201)]
    [InlineData(404)]
    public async Task HandlesStatusCodes(int statusCode)
    {
        // Arrange
        var handler = CreateFileResponseHandler(statusCode: (HttpStatusCode)statusCode);
        var fixture = CreateFixture(handler);
        var activity = new DownloadHttpFile
        {
            Url = new(new Uri("https://example.com/file.pdf")),
            Method = new("GET"),
            ExpectedStatusCodes = new([statusCode])
        };

        // Act
        var result = await fixture.RunActivityAsync(activity);

        // Assert
        var actualStatusCode = result.GetActivityOutput<int>(activity, nameof(DownloadHttpFile.StatusCode));
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
        var fixture = CreateFixture(handler);
        var activity = new DownloadHttpFile
        {
            Url = new(new Uri("https://api.example.com/download")),
            Method = new("GET"),
            ExpectedStatusCodes = new([200])
        };

        // Act
        var result = await fixture.RunActivityAsync(activity);

        // Assert
        var responseHeaders = result.GetActivityOutput<HttpHeaders>(activity, nameof(DownloadHttpFile.ResponseHeaders));
        Assert.NotNull(responseHeaders);
        Assert.Contains(responseHeaders.Keys, k => k.Equals("X-Rate-Limit", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(responseHeaders.Keys, k => k.Equals("X-Request-Id", StringComparison.OrdinalIgnoreCase));
    }

    private WorkflowTestFixture CreateFixture(HttpMessageHandler handler)
    {
        return new WorkflowTestFixture(testOutputHelper)
            .ConfigureElsa(elsa => elsa.UseHttp(http =>
            {
                http.HttpClientBuilder = builder => builder.ConfigurePrimaryHttpMessageHandler(() => handler);
            }));
    }

    private static HttpMessageHandler CreateFileResponseHandler(
        byte[]? content = null,
        string? filename = null,
        string? contentType = null,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        Dictionary<string, string>? additionalHeaders = null)
    {
        return new TestHttpMessageHandler((_, _) =>
        {
            var response = new HttpResponseMessage(statusCode);

            if (content != null || statusCode == HttpStatusCode.OK)
            {
                var actualContent = content ?? "Default file content"u8.ToArray();
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
            }

            if (additionalHeaders != null)
            {
                foreach (var header in additionalHeaders)
                {
                    response.Headers.Add(header.Key, header.Value);
                }
            }

            return Task.FromResult(response);
        });
    }

    private static HttpMessageHandler CreateCapturingHandler(byte[] content, HttpRequestMessage?[] capture)
    {
        return new TestHttpMessageHandler((request, _) =>
        {
            capture[0] = request;

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(content)
            };
            response.Content.Headers.ContentType = new("application/octet-stream");

            return Task.FromResult(response);
        });
    }

    private sealed class TestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return handler(request, cancellationToken);
        }
    }
}
