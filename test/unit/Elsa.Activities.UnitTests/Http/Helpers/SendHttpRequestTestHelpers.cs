using System.Net;

namespace Elsa.Activities.UnitTests.Http.Helpers;

/// <summary>
/// Shared helper methods for testing SendHttpRequest and FlowSendHttpRequest activities.
/// </summary>
public static class SendHttpRequestTestHelpers
{
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

    /// <summary>
    /// Captures HTTP request details during test execution.
    /// </summary>
    public sealed class RequestCapture
    {
        public HttpRequestMessage? CapturedRequest { get; set; }
    }
}
