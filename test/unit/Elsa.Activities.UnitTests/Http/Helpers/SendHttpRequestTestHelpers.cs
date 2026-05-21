using System.Diagnostics;
using System.Net;
using Xunit;

namespace Elsa.Activities.UnitTests.Http.Helpers;

/// <summary>
/// Shared helper methods for testing SendHttpRequest and FlowSendHttpRequest activities.
/// </summary>
public static class SendHttpRequestTestHelpers
{
    private const string TestActivitySourceName = "Elsa.Tests";

    /// <summary>
    /// Creates a response handler that returns a specific HTTP status code and optional content.
    /// </summary>
    public static Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> CreateResponseHandler(
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

    /// <summary>
    /// Creates an exception handler that throws a specific exception type with a message.
    /// </summary>
    public static Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> CreateExceptionHandler<TException>(string message)
        where TException : Exception
    {
        return (_, _) => throw ((TException)Activator.CreateInstance(typeof(TException), message)!);
    }

    public static async Task AssertPropagatesCurrentTraceContextAsync(
        Func<Uri, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>, Task> executeActivityAsync)
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == TestActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        using var source = new ActivitySource(TestActivitySourceName);
        using var parentActivity = source.StartActivity("parent");
        Assert.NotNull(parentActivity);

        var requestCapture = new RequestCapture();
        var responseHandler = CreateResponseHandler(HttpStatusCode.OK, "{}", requestCapture);

        await executeActivityAsync(new Uri("https://api.example.com/traced"), responseHandler);

        Assert.NotNull(requestCapture.CapturedRequest);
        Assert.True(requestCapture.CapturedRequest.Headers.TryGetValues("traceparent", out var traceParents));
        var traceParent = Assert.Single(traceParents);
        var traceParentParts = traceParent.Split('-');
        Assert.Equal(4, traceParentParts.Length);
        Assert.Equal("00", traceParentParts[0]);
        Assert.Equal(parentActivity.TraceId.ToString(), traceParentParts[1]);
    }

    /// <summary>
    /// Captures HTTP request details during test execution.
    /// </summary>
    public sealed class RequestCapture
    {
        public HttpRequestMessage? CapturedRequest { get; set; }
    }
}
