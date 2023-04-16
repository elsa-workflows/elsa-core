using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Http.ContentWriters;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using JetBrains.Annotations;
using HttpRequestHeaders = Elsa.Http.Models.HttpRequestHeaders;

namespace Elsa.Http;

/// <summary>
/// Send an HTTP request.
/// </summary>
[Activity("Elsa", "HTTP", "Send an HTTP request.", DisplayName = "Flow HTTP Request", Kind = ActivityKind.Task)]
[PublicAPI]
public class FlowSendHttpRequest : SendHttpRequestBase
{
    /// <summary>
    /// A list of expected status codes to handle.
    /// </summary>
    [Input(Description = "A list of expected status codes to handle.", UIHint = InputUIHints.MultiText)]
    public Input<ICollection<int>> ExpectedStatusCodes { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask HandleResponseAsync(ActivityExecutionContext context, HttpResponseMessage response)
    {
        var expectedStatusCodes = ExpectedStatusCodes.GetOrDefault(context) ?? new List<int>(0);
        var statusCode = (int)response.StatusCode;
        var hasMatchingStatusCode = expectedStatusCodes.Contains(statusCode);
        var outcome = expectedStatusCodes.Any() ? hasMatchingStatusCode ? statusCode.ToString() : "Unmatched status code" : "Done";

        await context.CompleteActivityWithOutcomesAsync(outcome);
    }
}

/// <summary>
/// Send an HTTP request.
/// </summary>
[Activity("Elsa", "HTTP", "Send an HTTP request.", DisplayName = "HTTP Request", Kind = ActivityKind.Task)]
[PublicAPI]
public class SendHttpRequest : SendHttpRequestBase
{
    /// <summary>
    /// A list of expected status codes to handle and the corresponding activity to execute when the status code matches.
    /// </summary>
    [Input(
        Description = "A list of expected status codes to handle and the corresponding activity to execute when the status code matches.",
        UIHint = "http-status-codes"
    )]
    public ICollection<HttpStatusCodeCase> ExpectedStatusCodes { get; set; } = new List<HttpStatusCodeCase>();

    /// <summary>
    /// The activity to execute when the HTTP status code does not match any of the expected status codes.
    /// </summary>
    [Port]
    public IActivity? UnmatchedStatusCode { get; set; }

    /// <inheritdoc />
    protected override async ValueTask HandleResponseAsync(ActivityExecutionContext context, HttpResponseMessage response)
    {
        var expectedStatusCodes = ExpectedStatusCodes;
        var statusCode = (int)response.StatusCode;
        var matchingCase = expectedStatusCodes.FirstOrDefault(x => x.StatusCode == statusCode);
        var activity = matchingCase?.Activity ?? UnmatchedStatusCode;

        await context.ScheduleActivityAsync(activity, OnChildActivityCompletedAsync);
    }

    private async ValueTask OnChildActivityCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        await context.CompleteActivityAsync();
    }
}

/// <summary>
/// A binding between an HTTP status code and an activity.
/// </summary>
public class HttpStatusCodeCase
{
    /// <summary>
    /// Creates a new instance of the <see cref="HttpStatusCodeCase"/> class.
    /// </summary>
    [JsonConstructor]
    public HttpStatusCodeCase()
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="HttpStatusCodeCase"/> class.
    /// </summary>
    public HttpStatusCodeCase(int statusCode, IActivity activity)
    {
        StatusCode = statusCode;
        Activity = activity;
    }

    /// <summary>
    /// The HTTP status code to match.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// The activity to execute when the HTTP status code matches.
    /// </summary>
    public IActivity? Activity { get; set; }
}

/// <summary>
/// Base class for activities that send HTTP requests.
/// </summary>
public abstract class SendHttpRequestBase : Activity<HttpResponseMessage>
{
    /// <summary>
    /// The URL to send the request to.
    /// </summary>
    [Input]
    public Input<Uri?> Url { get; set; } = default!;

    /// <summary>
    /// The HTTP method to use when sending the request.
    /// </summary>
    [Input(
        Description = "The HTTP method to use when sending the request.",
        Options = new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS", "HEAD" },
        DefaultValue = "GET",
        UIHint = InputUIHints.Dropdown
    )]
    public Input<string> Method { get; set; } = new("GET");

    /// <summary>
    /// The content to send with the request. Can be a string, an object, a byte array or a stream.
    /// </summary>
    [Input(Description = "The content to send with the request. Can be a string, an object, a byte array or a stream.")]
    public Input<object?> Content { get; set; } = default!;

    /// <summary>
    /// The content type to use when sending the request.
    /// </summary>
    [Input(
        Description = "The content type to use when sending the request.",
        Options = new[] { "", "text/plain", "text/html", "application/json", "application/xml", "application/x-www-form-urlencoded" },
        UIHint = InputUIHints.Dropdown
    )]
    public Input<string?> ContentType { get; set; } = default!;

    /// <summary>
    /// The Authorization header value to send with the request.
    /// </summary>
    /// <example>Bearer {some-access-token}</example>
    [Input(
        Description = "The Authorization header value to send with the request. For example: Bearer {some-access-token}",
        Category = "Security"
    )]
    public Input<string?> Authorization { get; set; } = default!;

    /// <summary>
    /// The headers to send along with the request.
    /// </summary>
    [Input(Description = "The headers to send along with the request.", Category = "Advanced")]
    public Input<HttpRequestHeaders?> RequestHeaders { get; set; } = new(new HttpRequestHeaders());

    /// <summary>
    /// The parsed content, if any.
    /// </summary>
    [Output(Description = "The parsed content, if any.")]
    public Output<object?> ParsedContent { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        await TrySendAsync(context);
    }

    /// <summary>
    /// Handles the response.
    /// </summary>
    protected abstract ValueTask HandleResponseAsync(ActivityExecutionContext context, HttpResponseMessage response);

    private async Task TrySendAsync(ActivityExecutionContext context)
    {
        var request = PrepareRequest(context);
        var httpClientFactory = context.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(nameof(SendHttpRequestBase));
        var cancellationToken = context.CancellationToken;

        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);
            var parsedContent = await ParseContentAsync(context, response.Content);
            context.Set(Result, response);
            context.Set(ParsedContent, parsedContent);

            await HandleResponseAsync(context, response);
        }
        catch (TaskCanceledException e)
        {
            context.JournalData.Add("Cancelled", true);
        }
    }

    private async Task<object?> ParseContentAsync(ActivityExecutionContext context, HttpContent httpContent)
    {
        if (!HasContent(httpContent))
            return null;

        var cancellationToken = context.CancellationToken;
        var targetType = ParsedContent.GetTargetType(context);
        var contentStream = await httpContent.ReadAsStreamAsync(cancellationToken);
        var contentType = httpContent.Headers.ContentType?.MediaType!;

        return await context.ParseContentAsync(contentStream, contentType, targetType, cancellationToken);
    }

    private static bool HasContent(HttpContent httpContent) => httpContent.Headers.ContentLength > 0;

    private HttpRequestMessage PrepareRequest(ActivityExecutionContext context)
    {
        var method = Method.GetOrDefault(context) ?? "GET";
        var url = Url.Get(context);
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        var headers = context.GetHeaders(RequestHeaders);
        var authorization = Authorization.GetOrDefault(context);

        if (!string.IsNullOrWhiteSpace(authorization))
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(authorization);

        foreach (var header in headers)
            request.Headers.Add(header.Key, header.Value.AsEnumerable());

        var contentType = ContentType.GetOrDefault(context);
        var content = Content.GetOrDefault(context);

        if (contentType != null && content != null)
        {
            var contentWriters = context.GetServices<IHttpContentFactory>();
            var contentWriter = SelectContentWriter(contentType, contentWriters);
            request.Content = contentWriter.CreateHttpContent(content, contentType);
        }

        return request;
    }

    private IHttpContentFactory SelectContentWriter(string? contentType, IEnumerable<IHttpContentFactory> requestContentWriters) =>
        string.IsNullOrWhiteSpace(contentType) ? new JsonContentFactory() : requestContentWriters.First(w => w.SupportsContentType(contentType));
}