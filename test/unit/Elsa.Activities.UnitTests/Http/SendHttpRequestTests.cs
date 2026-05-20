using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Elsa.Activities.UnitTests.Http.Helpers;
using Elsa.Extensions;
using Elsa.Http;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Activities.UnitTests.Http;

public class SendHttpRequestTests
{
    private const string TestActivitySourceName = "Elsa.Tests";

    [Theory]
    [InlineData("GET", "https://api.example.com/data", "{\"result\": \"success\"}", 200)]
    [InlineData("POST", "https://api.example.com/create", "{\"id\": 123}", 201)]
    [InlineData("PUT", "https://api.example.com/update", "{\"updated\": true}", 200)]
    public async Task Should_Send_Request_And_Handle_Success_Response(string method, string url, string jsonResponse, int expectedStatusCode)
    {
        // Arrange
        var expectedUrl = new Uri(url);
        var expectedMethod = new HttpMethod(method);
        var expectedHttpStatusCode = (HttpStatusCode)expectedStatusCode;
        var requestCapture = new RequestCapture();
        var responseHandler = CreateResponseHandler(expectedHttpStatusCode, jsonResponse, requestCapture);
        var sendHttpRequest = CreateSendHttpRequest(expectedUrl, method);

        // Act
        var context = await ExecuteActivityAsync(sendHttpRequest, responseHandler);

        // Assert
        Assert.NotNull(requestCapture.CapturedRequest);
        Assert.Equal(expectedMethod, requestCapture.CapturedRequest.Method);
        Assert.Equal(expectedUrl, requestCapture.CapturedRequest.RequestUri);
        
        var statusCodeOutput = context.GetActivityOutput(() => sendHttpRequest.StatusCode);
        Assert.Equal(expectedStatusCode, statusCodeOutput);
    }

    [Theory]
    [InlineData("Bearer token123")]
    [InlineData("Basic YWRtaW46cGFzcw==")]
    [InlineData("ApiKey abc123")]
    public async Task Should_Add_Authorization_Header(string authorizationHeader)
    {
        // Arrange
        var expectedUrl = new Uri("https://api.example.com/secure");
        
        var requestCapture = new RequestCapture();
        var responseHandler = CreateResponseHandler(HttpStatusCode.OK, null, requestCapture);
        var sendHttpRequest = CreateSendHttpRequest(expectedUrl, authorization: authorizationHeader);

        // Act
        await ExecuteActivityAsync(sendHttpRequest, responseHandler);

        // Assert
        Assert.NotNull(requestCapture.CapturedRequest);
        Assert.NotNull(requestCapture.CapturedRequest.Headers.Authorization);
        Assert.Equal(authorizationHeader, requestCapture.CapturedRequest.Headers.Authorization.ToString());
    }

    [Fact]
    public async Task Should_Propagate_Current_Trace_Context()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == TestActivitySourceName || source.Name == "System.Net.Http",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        using var source = new ActivitySource(TestActivitySourceName);
        using var parentActivity = source.StartActivity("parent");
        await using var server = new TraceContextServer();
        using var httpClient = new HttpClient();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);
        var sendHttpRequest = CreateSendHttpRequest(server.Uri);

        // Act
        await new ActivityTestFixture(sendHttpRequest)
            .WithHttpServices()
            .ConfigureServices(services => services.AddSingleton(httpClientFactory))
            .ExecuteAsync();
        var headers = await server.GetRequestHeadersAsync();

        // Assert
        Assert.NotNull(parentActivity);
        Assert.True(headers.TryGetValue("traceparent", out var traceParent));
        var traceParentParts = traceParent.Split('-');
        Assert.Equal(4, traceParentParts.Length);
        Assert.Equal("00", traceParentParts[0]);
        Assert.Equal(parentActivity.TraceId.ToString(), traceParentParts[1]);
        Assert.NotEqual(parentActivity.SpanId.ToString(), traceParentParts[2]);
    }

    [Theory]
    [InlineData(new[]{200, 404}, new[]{"mockActivity200", "mockActivity404"}, "mockUnmatchedActivity", HttpStatusCode.NotFound, "mockActivity404")]
    [InlineData(new[]{200, 404}, new[]{"mockActivity200", "mockActivity404"}, "mockUnmatchedActivity", HttpStatusCode.InternalServerError, "mockUnmatchedActivity")]
    public async Task Should_Schedule_Activity_According_To_Handlers(int[] statusCodes, string[] activityNames, string handler, HttpStatusCode expectedStatusCode, string expectedScheduledActivityName)
    {
        // Arrange
        var (sendHttpRequest, childActivities) = CreateSendHttpRequestWithStatusHandlers(
            expectedStatusCodes: [(statusCodes[0], activityNames[0]), (statusCodes[1], activityNames[1])],
            unmatchedHandler: handler
        );

        var responseHandler = CreateResponseHandler(expectedStatusCode);

        // Act
        var context = await ExecuteActivityAsync(sendHttpRequest, responseHandler);

        // Assert that the correct activity was scheduled.
        var expectedActivity = childActivities[expectedScheduledActivityName];
        var hasScheduledActivity = context.HasScheduledActivity(expectedActivity);
        Assert.True(hasScheduledActivity);
    }

    [Fact]
    public async Task Should_Schedule_FailedToConnect_Activity_On_HttpRequestException()
    {
        // Arrange
        var (sendHttpRequest, childActivities) = CreateSendHttpRequestWithErrorHandlers(
            failedToConnect: "mockFailedToConnect"
        );
        
        var responseHandler = CreateExceptionHandler<HttpRequestException>("Connection failed");

        // Act
        var context = await ExecuteActivityAsync(sendHttpRequest, responseHandler);

        // Assert
        var expectedScheduledActivity = childActivities["mockFailedToConnect"];
        var hasScheduledExpectedActivity = context.HasScheduledActivity(expectedScheduledActivity);
        Assert.True(hasScheduledExpectedActivity);
    }

    [Fact]
    public async Task Should_Schedule_Timeout_Activity_On_TaskCanceledException()
    {
        // Arrange
        var (sendHttpRequest, childActivities) = CreateSendHttpRequestWithErrorHandlers(
            timeout: "mockTimeout"
        );
        
        var responseHandler = CreateExceptionHandler<TaskCanceledException>("Request timed out");

        // Act
        var context = await ExecuteActivityAsync(sendHttpRequest, responseHandler);

        // Assert
        var expectedScheduledActivity = childActivities["mockTimeout"];
        var hasScheduledExpectedActivity = context.HasScheduledActivity(expectedScheduledActivity);
        Assert.True(hasScheduledExpectedActivity);
    }

    [Fact]
    public async Task Should_Schedule_No_Activity_When_No_Status_Code_Cases_Match_And_No_Unmatched_Handler()
    {
        // Arrange
        var (configured, _) = CreateSendHttpRequestWithStatusHandlers([(200, "handler200")], unmatchedHandler: null); 
        var responseHandler = CreateResponseHandler(HttpStatusCode.InternalServerError); // 500 - no match

        // Act
        var context = await ExecuteActivityAsync(configured, responseHandler);

        // Assert
        var allScheduledActivities = context.WorkflowExecutionContext.Scheduler.List().ToList();
        Assert.Empty(allScheduledActivities);
    }

    [Fact]
    public async Task Should_Set_Response_Headers_Output()
    {
        // Arrange
        var expectedHeaders = new Dictionary<string, string>
        {
            { "Custom-Header", "CustomValue" },
            { "X-Rate-Limit", "100" }
        };

        var responseHandler = CreateResponseHandler(HttpStatusCode.OK, additionalHeaders: expectedHeaders);
        var sendHttpRequest = CreateSendHttpRequest(new("https://api.example.com/headers"));

        // Act
        var context = await ExecuteActivityAsync(sendHttpRequest, responseHandler);

        // Assert
        var responseHeadersObj = context.GetActivityOutput(() => sendHttpRequest.ResponseHeaders);
        var responseHeaders = responseHeadersObj as HttpHeaders;
        Assert.NotNull(responseHeaders);
        Assert.True(responseHeaders.ContainsKey("Custom-Header"));
        Assert.True(responseHeaders.ContainsKey("X-Rate-Limit"));
    }

    [Fact]
    public void Should_Have_Correct_Activity_Attributes()
    {
        var fixture = new ActivityTestFixture(new SendHttpRequest());
        fixture.AssertActivityAttributes(
            expectedNamespace: "Elsa",
            expectedCategory: "HTTP",
            expectedDisplayName: "HTTP Request",
            expectedDescription: "Send an HTTP request.",
            expectedKind: Elsa.Workflows.ActivityKind.Task
        );
    }

    // Private helper methods placed after all public members
    private static Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> CreateResponseHandler(
        HttpStatusCode statusCode,
        string? content = null,
        RequestCapture? requestCapture = null,
        Dictionary<string, string>? additionalHeaders = null)
    {
        return (request, _) =>
        {
            if (requestCapture != null)
                requestCapture.CapturedRequest = request;

            return Task.FromResult(ActivityTestFixtureHttpExtensions.CreateHttpResponse(statusCode, content, additionalHeaders));
        };
    }

    private sealed class RequestCapture
    {
        public HttpRequestMessage? CapturedRequest { get; set; }
    }

    private sealed class TraceContextServer : IAsyncDisposable
    {
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan HeaderReadTimeout = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan TeardownTimeout = TimeSpan.FromSeconds(2);
        private const int MaxHeaderLineLength = 16 * 1024;

        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly Task<Dictionary<string, string>> _requestTask;

        public TraceContextServer()
        {
            _listener.Start();
            var port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            Uri = new($"http://127.0.0.1:{port}/traced");
            _requestTask = AcceptRequestAsync();
        }

        public Uri Uri { get; }

        public async Task<Dictionary<string, string>> GetRequestHeadersAsync()
        {
            try
            {
                return await _requestTask.WaitAsync(RequestTimeout);
            }
            catch (TimeoutException ex)
            {
                throw new TimeoutException("The trace context test server did not receive an HTTP request before the timeout elapsed.", ex);
            }
        }

        public async ValueTask DisposeAsync()
        {
            using var cancellationTokenSource = _cancellationTokenSource;
            var requestTaskCompleted = _requestTask.IsCompleted;
            cancellationTokenSource.Cancel();
            _listener.Stop();

            try
            {
                await _requestTask.WaitAsync(TeardownTimeout).ConfigureAwait(false);
            }
            catch (Exception ex) when (!requestTaskCompleted && IsExpectedTeardownException(ex))
            {
                Trace.WriteLine($"TraceContextServer teardown ignored exception: {ex}");
            }
        }

        private async Task<Dictionary<string, string>> AcceptRequestAsync()
        {
            using var client = await _listener.AcceptTcpClientAsync(_cancellationTokenSource.Token);
            await using var stream = client.GetStream();
            await using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { NewLine = "\r\n", AutoFlush = true };
            var headers = await ReadHeadersAsync(stream, _cancellationTokenSource.Token);

            await writer.WriteAsync("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nContent-Type: application/json\r\nConnection: close\r\n\r\n{}");
            return headers;
        }

        private static async Task<Dictionary<string, string>> ReadHeadersAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (await ReadLineAsync(stream, cancellationToken) == null)
                throw new IOException("The HTTP client closed the connection before sending a request line.");

            while (await ReadLineAsync(stream, cancellationToken) is { } line)
            {
                if (line.Length == 0)
                    return headers;

                var separator = line.IndexOf(':');

                if (separator > 0)
                    headers[line[..separator]] = line[(separator + 1)..].Trim();
            }

            throw new IOException("The HTTP client closed the connection before completing the request headers.");
        }

        private static async Task<string?> ReadLineAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            var line = new List<byte>();
            var buffer = new byte[1];

            while (true)
            {
                var bytesRead = await ReadWithTimeoutAsync(stream, buffer, cancellationToken);

                if (bytesRead == 0)
                    return line.Count == 0 ? null : Encoding.ASCII.GetString(line.ToArray());

                var value = buffer[0];

                if (value == '\n')
                {
                    if (line.Count > 0 && line[^1] == '\r')
                        line.RemoveAt(line.Count - 1);

                    return Encoding.ASCII.GetString(line.ToArray());
                }

                line.Add(value);

                if (line.Count > MaxHeaderLineLength)
                    throw new InvalidDataException($"The HTTP request header line exceeded {MaxHeaderLineLength} bytes.");
            }
        }

        private static async Task<int> ReadWithTimeoutAsync(NetworkStream stream, byte[] buffer, CancellationToken cancellationToken)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(HeaderReadTimeout);

            try
            {
                return await stream.ReadAsync(buffer.AsMemory(0, 1), timeout.Token);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException("Timed out while reading the HTTP request headers.", ex);
            }
        }

        private static bool IsExpectedTeardownException(Exception exception)
        {
            return exception is OperationCanceledException or ObjectDisposedException or SocketException or TimeoutException ||
                   exception.InnerException is not null && IsExpectedTeardownException(exception.InnerException);
        }
    }

    private static SendHttpRequest CreateSendHttpRequest(
        Uri url,
        string method = "GET",
        object? content = null,
        string? contentType = null,
        string? authorization = null)
    {
        return new()
        {
            Url = new(url),
            Method = new(method),
            Content = content != null ? new Input<object?>(content) : null!,
            ContentType = contentType != null ? new Input<string?>(contentType) : null!,
            Authorization = authorization != null ? new Input<string?>(authorization) : null!,
            ExpectedStatusCodes = new List<HttpStatusCodeCase>()
        };
    }

    private static Task<ActivityExecutionContext> ExecuteActivityAsync(
        SendHttpRequest sendHttpRequest,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseHandler)
    {
        return new ActivityTestFixture(sendHttpRequest).WithHttpServices(responseHandler).ExecuteAsync();
    }

    private static (SendHttpRequest sendHttpRequest, Dictionary<string, IActivity> childActivities) CreateSendHttpRequestWithStatusHandlers(
        (int statusCode, string activityName)[] expectedStatusCodes,
        string? unmatchedHandler)
    {
        var childActivities = new Dictionary<string, IActivity>();
        
        // Create mock activities for expected status codes
        var expectedStatusCodeCases = expectedStatusCodes.Select(x => 
        {
            var mockActivity = Substitute.For<IActivity>();
            childActivities[x.activityName] = mockActivity;
            return new HttpStatusCodeCase(x.statusCode, mockActivity);
        }).ToList();
        
        // Create mock activity for unmatched handler
        var unmatchedActivity = Substitute.For<IActivity>();
        if (unmatchedHandler is not null)
        {
            childActivities[unmatchedHandler] = unmatchedActivity;
        }

        var sendHttpRequest = new SendHttpRequest
        {
            Url = new(new Uri("https://api.example.com/test")),
            Method = new("GET"),
            ExpectedStatusCodes = expectedStatusCodeCases,
            UnmatchedStatusCode = unmatchedHandler is not null ? unmatchedActivity : null
        };

        return (sendHttpRequest, childActivities);
    }

    private static (SendHttpRequest sendHttpRequest, Dictionary<string, IActivity> childActivities) CreateSendHttpRequestWithErrorHandlers(
        string? failedToConnect = null,
        string? timeout = null)
    {
        var childActivities = new Dictionary<string, IActivity>();
        
        IActivity? failedToConnectActivity = null;
        IActivity? timeoutActivity = null;
        
        if (failedToConnect != null)
        {
            failedToConnectActivity = Substitute.For<IActivity>();
            childActivities[failedToConnect] = failedToConnectActivity;
        }
        
        if (timeout != null)
        {
            timeoutActivity = Substitute.For<IActivity>();
            childActivities[timeout] = timeoutActivity;
        }

        var sendHttpRequest = new SendHttpRequest
        {
            Url = new(new Uri("https://api.example.com/error")),
            Method = new("GET"),
            ExpectedStatusCodes = new List<HttpStatusCodeCase>(),
            FailedToConnect = failedToConnectActivity,
            Timeout = timeoutActivity
        };

        return (sendHttpRequest, childActivities);
    }

    private static Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> CreateExceptionHandler<TException>(string message) 
        where TException : Exception
    {
        return (_, _) => throw ((TException)Activator.CreateInstance(typeof(TException), message)!);
    }
}
