using System.Net;
using Elsa.Activities.UnitTests.Http.Helpers;
using Elsa.Extensions;
using Elsa.Http;
using Elsa.Testing.Shared;
using Elsa.Workflows;

namespace Elsa.Activities.UnitTests.Http;

public class DownloadHttpFileTests
{
    [Theory]
    [InlineData("GET", "https://example.com/file.pdf")]
    [InlineData("POST", "https://api.example.com/download")]
    [InlineData("PUT", "https://api.example.com/update-file")]
    public async Task ExecuteAsync_SendsRequest_WithCorrectMethodAndUrl(string method, string url)
    {
        // Arrange
        var expectedUrl = new Uri(url);
        var expectedMethod = new HttpMethod(method);
        var requestCapture = new RequestCapture();
        var responseHandler = CreateFileResponseHandler(requestCapture: requestCapture);
        var activity = CreateActivity(expectedUrl, method);

        // Act
        await ExecuteAsync(activity, responseHandler);

        // Assert
        Assert.NotNull(requestCapture.CapturedRequest);
        Assert.Equal(expectedMethod, requestCapture.CapturedRequest.Method);
        Assert.Equal(expectedUrl, requestCapture.CapturedRequest.RequestUri);
    }

    [Theory]
    [InlineData("Bearer token123")]
    [InlineData("Basic YWRtaW46cGFzcw==")]
    public async Task ExecuteAsync_AddsAuthorizationHeader_WhenProvided(string authorizationHeader)
    {
        // Arrange
        var requestCapture = new RequestCapture();
        var responseHandler = CreateFileResponseHandler(requestCapture: requestCapture);
        var activity = CreateActivity(new("https://example.com/file.pdf"), authorization: authorizationHeader);

        // Act
        await ExecuteAsync(activity, responseHandler);

        // Assert
        Assert.NotNull(requestCapture.CapturedRequest);
        Assert.NotNull(requestCapture.CapturedRequest.Headers.Authorization);
        Assert.Equal(authorizationHeader, requestCapture.CapturedRequest.Headers.Authorization.ToString());
    }

    [Theory]
    [InlineData(200)]
    [InlineData(201)]
    [InlineData(404)]
    public async Task ExecuteAsync_SetsStatusCodeOutput(int statusCode)
    {
        // Arrange
        var responseHandler = CreateFileResponseHandler(statusCode: (HttpStatusCode)statusCode);
        var activity = CreateActivity(new("https://example.com/file.pdf"));

        // Act
        var context = await ExecuteAsync(activity, responseHandler);

        // Assert
        var actualStatusCode = context.GetActivityOutput(() => activity.StatusCode);
        Assert.Equal(statusCode, actualStatusCode);
    }

    [Fact]
    public async Task ExecuteAsync_SetsFileOutput_WithCorrectFilename()
    {
        // Arrange
        const string expectedFilename = "document.pdf";
        var responseHandler = CreateFileResponseHandler(filename: expectedFilename);
        var activity = CreateActivity(new("https://example.com/file.pdf"));

        // Act
        var context = await ExecuteAsync(activity, responseHandler);

        // Assert
        var file = context.GetActivityOutput(() => activity.Result) as HttpFile;
        Assert.NotNull(file);
        Assert.Equal(expectedFilename, file.Filename);
    }

    [Fact]
    public async Task ExecuteAsync_SetsFileOutput_WithCorrectContentType()
    {
        // Arrange
        const string expectedContentType = "application/pdf";
        var responseHandler = CreateFileResponseHandler(contentType: expectedContentType);
        var activity = CreateActivity(new("https://example.com/file.pdf"));

        // Act
        var context = await ExecuteAsync(activity, responseHandler);

        // Assert
        var file = context.GetActivityOutput(() => activity.Result) as HttpFile;
        Assert.NotNull(file);
        Assert.Equal(expectedContentType, file.ContentType);
    }

    [Fact]
    public async Task ExecuteAsync_SetsResponseContentStream_FromFile()
    {
        // Arrange
        var fileContent = "Test file content"u8.ToArray();
        var responseHandler = CreateFileResponseHandler(content: fileContent);
        var activity = CreateActivity(new("https://example.com/file.txt"));

        // Act
        var context = await ExecuteAsync(activity, responseHandler);

        // Assert
        var stream = context.GetActivityOutput(() => activity.ResponseContentStream) as Stream;
        Assert.NotNull(stream);

        using var reader = new StreamReader(stream);
        var actualContent = await reader.ReadToEndAsync();
        Assert.Equal("Test file content", actualContent);
    }

    [Fact]
    public async Task ExecuteAsync_SetsResponseHeaders()
    {
        // Arrange
        var expectedHeaders = new Dictionary<string, string>
        {
            { "X-Custom-Header", "CustomValue" },
            { "X-Request-Id", "12345" }
        };
        var responseHandler = CreateFileResponseHandler(additionalHeaders: expectedHeaders);
        var activity = CreateActivity(new("https://example.com/file.pdf"));

        // Act
        var context = await ExecuteAsync(activity, responseHandler);

        // Assert
        var responseHeaders = context.GetActivityOutput(() => activity.ResponseHeaders);
        Assert.NotNull(responseHeaders);
        var httpHeaders = Assert.IsType<HttpHeaders>(responseHeaders);
        Assert.True(httpHeaders.Count > 0);
        Assert.Contains(httpHeaders.Keys, k => k.Equals("X-Custom-Header", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(httpHeaders.Keys, k => k.Equals("X-Request-Id", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteAsync_CompletesSuccessfully()
    {
        // Arrange
        var responseHandler = CreateFileResponseHandler();
        var activity = CreateActivity(new("https://example.com/file.pdf"));

        // Act
        var context = await ExecuteAsync(activity, responseHandler);

        // Assert
        Assert.Equal(ActivityStatus.Completed, context.Status);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesHttpRequestException()
    {
        // Arrange
        var responseHandler = CreateExceptionHandler<HttpRequestException>("Connection failed");
        var activity = CreateActivity(new("https://example.com/file.pdf"));

        // Act
        var context = await ExecuteAsync(activity, responseHandler);

        // Assert
        Assert.Equal(ActivityStatus.Completed, context.Status);
        Assert.True(context.JournalData.ContainsKey("Error"));
    }

    [Fact]
    public async Task ExecuteAsync_HandlesTaskCanceledException()
    {
        // Arrange
        var responseHandler = CreateExceptionHandler<TaskCanceledException>("Request timed out");
        var activity = CreateActivity(new("https://example.com/file.pdf"));

        // Act
        var context = await ExecuteAsync(activity, responseHandler);

        // Assert
        Assert.Equal(ActivityStatus.Completed, context.Status);
        Assert.True(context.JournalData.ContainsKey("Cancelled"));
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsNull_WhenResponseHasNoContent()
    {
        // Arrange
        var responseHandler = CreateEmptyResponseHandler();
        var activity = CreateActivity(new("https://example.com/empty"));

        // Act
        var context = await ExecuteAsync(activity, responseHandler);

        // Assert
        var file = context.GetActivityOutput(() => activity.Result);
        Assert.Null(file);

        var stream = context.GetActivityOutput(() => activity.ResponseContentStream);
        Assert.Null(stream);
    }

    [Fact]
    public async Task ExecuteAsync_ExtractsFilename_FromContentDisposition()
    {
        // Arrange
        const string expectedFilename = "report.xlsx";
        var responseHandler = CreateFileResponseHandler(filename: expectedFilename);
        var activity = CreateActivity(new("https://example.com/download"));

        // Act
        var context = await ExecuteAsync(activity, responseHandler);

        // Assert
        var file = context.GetActivityOutput(() => activity.Result) as HttpFile;
        Assert.NotNull(file);
        Assert.Equal(expectedFilename, file.Filename);
    }

    [Fact]
    public async Task ExecuteAsync_ExtractsFilename_FromUrlSegment_WhenNoContentDisposition()
    {
        // Arrange
        var responseHandler = CreateFileResponseHandler(filename: null); // No Content-Disposition
        var activity = CreateActivity(new("https://example.com/files/document.pdf"));

        // Act
        var context = await ExecuteAsync(activity, responseHandler);

        // Assert
        var file = context.GetActivityOutput(() => activity.Result) as HttpFile;
        Assert.NotNull(file);
        Assert.Equal("document.pdf", file.Filename);
    }

    [Theory]
    [InlineData("https://example.com", "/")]
    [InlineData("https://example.com/", "/")]
    [InlineData("https://example.com/download", "download")]
    public async Task ExecuteAsync_ExtractsFilename_FromUrlWhenNoContentDisposition(string url, string expectedFilename)
    {
        // Arrange
        var responseHandler = CreateFileResponseHandler(filename: null);
        var activity = CreateActivity(new Uri(url));

        // Act
        var context = await ExecuteAsync(activity, responseHandler);

        // Assert
        var file = context.GetActivityOutput(() => activity.Result) as HttpFile;
        Assert.NotNull(file);
        Assert.Equal(expectedFilename, file.Filename);
    }

    private static DownloadHttpFile CreateActivity(
        Uri url,
        string method = "GET",
        string? authorization = null) =>
        new()
        {
            Url = new(url),
            Method = new(method),
            Authorization = authorization != null ? new(authorization) : null!,
            ExpectedStatusCodes = new([200])
        };

    private static Task<ActivityExecutionContext> ExecuteAsync(
        DownloadHttpFile activity,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseHandler) =>
        new ActivityTestFixture(activity).WithHttpServices(responseHandler).ExecuteAsync();

    private static Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> CreateFileResponseHandler(
        HttpStatusCode statusCode = HttpStatusCode.OK,
        byte[]? content = null,
        string? filename = null,
        string? contentType = null,
        Dictionary<string, string>? additionalHeaders = null,
        RequestCapture? requestCapture = null)
    {
        return (request, _) =>
        {
            requestCapture?.CapturedRequest = request;

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
            {
                foreach (var header in additionalHeaders)
                {
                    response.Headers.Add(header.Key, header.Value);
                }
            }

            return Task.FromResult(response);
        };
    }

    private static Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> CreateExceptionHandler<TException>(string message)
        where TException : Exception =>
        (_, _) => throw (TException)Activator.CreateInstance(typeof(TException), message)!;

    private static Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> CreateEmptyResponseHandler()
    {
        return (_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            // Don't set Content at all, leaving it null
            response.Content = new ByteArrayContent(Array.Empty<byte>());
            response.Content.Headers.ContentLength = 0;
            return Task.FromResult(response);
        };
    }

    private sealed class RequestCapture
    {
        public HttpRequestMessage? CapturedRequest { get; set; }
    }
}
