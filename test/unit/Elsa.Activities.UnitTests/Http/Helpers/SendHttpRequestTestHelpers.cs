using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

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
        await Assert.That(parentActivity).IsNotNull();

        var requestCapture = new RequestCapture();
        var responseHandler = CreateResponseHandler(HttpStatusCode.OK, "{}", requestCapture);

        await executeActivityAsync(new Uri("https://api.example.com/traced"), responseHandler);

        await Assert.That(requestCapture.CapturedRequest).IsNotNull();
        await Assert.That(requestCapture.CapturedRequest.Headers.TryGetValues("traceparent", out var traceParents)).IsTrue();
        var traceParent = await Assert.That(traceParents).HasSingleItem();
        var traceParentParts = traceParent.Split('-');
        await Assert.That(traceParentParts.Length).IsEqualTo(4);
        await Assert.That(traceParentParts[0].Length).IsEqualTo(2);
        await Assert.That(traceParentParts[1]).IsEqualTo(parentActivity.TraceId.ToString());
    }

    /// <summary>
    /// Captures HTTP request details during test execution.
    /// </summary>
    public sealed class RequestCapture
    {
        public HttpRequestMessage? CapturedRequest { get; set; }
    }
}