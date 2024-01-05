using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Http.ContentWriters;
using Elsa.Http.UIHints;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.UIHints;
using Elsa.Workflows.Models;
using HttpHeaders = Elsa.Http.Models.HttpHeaders;

namespace Elsa.Http;

/// <summary>
/// Sends HTTP requests from a background task.
/// </summary>
[Activity("Elsa", "HTTP", "Send an HTTP request from a background task.", DisplayName = "HTTP Request Task", Kind = ActivityKind.Task)]
public class SendHttpRequestTask : Activity<HttpStatusCode>
{
    /// <inheritdoc />
    public SendHttpRequestTask([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <summary>
    /// The URL to send the request to.
    /// </summary>
    [Input(Description = "The URL to send the request to.")]
    public Input<Uri?> Url { get; set; } = default!;

    /// <summary>
    /// The HTTP method to use when sending the request.
    /// </summary>
    [Input(
        Description = "The HTTP method to use when sending the request.",
        Options = new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS", "HEAD" },
        DefaultValue = "GET",
        UIHint = InputUIHints.DropDown
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
        UIHandler = typeof(HttpContentTypeOptionsProvider),
        UIHint = InputUIHints.DropDown
    )]
    public Input<string?> ContentType { get; set; } = default!;

    /// <summary>
    /// The Authorization header value to send with the request.
    /// </summary>
    /// <example>Bearer {some-access-token}</example>
    [Input(Description = "The Authorization header value to send with the request. For example: Bearer {some-access-token}", Category = "Security")]
    public Input<string?> Authorization { get; set; } = default!;

    /// <summary>
    /// A list of expected status codes to handle.
    /// </summary>
    [Input(
        Description = "A list of expected status codes to handle.",
        UIHint = InputUIHints.MultiText,
        DefaultValueProvider = typeof(FlowSendHttpRequest)
    )]
    public Input<ICollection<int>> ExpectedStatusCodes { get; set; } = default!;

    /// <summary>
    /// A value that allows to add the Authorization header without validation.
    /// </summary>
    [Input(Description = "A value that allows to add the Authorization header without validation.", Category = "Security")]
    public Input<bool> DisableAuthorizationHeaderValidation { get; set; } = default!;

    /// <summary>
    /// The headers to send along with the request.
    /// </summary>
    [Input(
        Description = "The headers to send along with the request.",
        UIHint = InputUIHints.JsonEditor,
        Category = "Advanced"
    )]
    public Input<HttpHeaders?> RequestHeaders { get; set; } = new(new HttpHeaders());

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
            context.SetResult(response.StatusCode);
            context.Set(ParsedContent, parsedContent);

            await HandleResponseAsync(context, response);
        }
        catch (HttpRequestException e)
        {
            context.AddExecutionLogEntry("Error", e.Message, payload: new { StackTrace = e.StackTrace });
            context.JournalData.Add("Error", e.Message);
            await HandleRequestExceptionAsync(context, e);
        }
        catch (TaskCanceledException e)
        {
            context.AddExecutionLogEntry("Error", e.Message, payload: new { StackTrace = e.StackTrace });
            context.JournalData.Add("Cancelled", true);
            await HandleTaskCanceledExceptionAsync(context, e);
        }
    }
    
    private async Task HandleResponseAsync(ActivityExecutionContext context, HttpResponseMessage response)
    {
        var expectedStatusCodes = ExpectedStatusCodes.GetOrDefault(context) ?? new List<int>(0);
        var statusCode = (int)response.StatusCode;
        var hasMatchingStatusCode = expectedStatusCodes.Contains(statusCode);
        var outcome = expectedStatusCodes.Any() ? hasMatchingStatusCode ? statusCode.ToString() : "Unmatched status code" : default;
        var outcomes = new List<string>();

        if (outcome != null)
            outcomes.Add(outcome);

        outcomes.Add("Done");
        context.JournalData["StatusCode"] = statusCode;
        await context.CompleteActivityWithOutcomesAsync(outcomes.ToArray());
    }

    private async Task HandleRequestExceptionAsync(ActivityExecutionContext context, HttpRequestException exception)
    {
        await context.CompleteActivityWithOutcomesAsync("Failed to connect");
    }

    private async Task HandleTaskCanceledExceptionAsync(ActivityExecutionContext context, TaskCanceledException exception)
    {
        await context.CompleteActivityWithOutcomesAsync("Timeout");
    }

    private async Task<object?> ParseContentAsync(ActivityExecutionContext context, HttpContent httpContent)
    {
        if (!HasContent(httpContent))
            return null;

        var cancellationToken = context.CancellationToken;
        var targetType = ParsedContent.GetTargetType(context);
        var contentStream = await httpContent.ReadAsStreamAsync(cancellationToken);
        var contentType = httpContent.Headers.ContentType?.MediaType!;

        targetType ??= contentType switch
        {
            "application/json" => typeof(object),
            _ => typeof(string)
        };

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
        var addAuthorizationWithoutValidation = DisableAuthorizationHeaderValidation.GetOrDefault(context);

        if (!string.IsNullOrWhiteSpace(authorization))
            if (addAuthorizationWithoutValidation)
                request.Headers.TryAddWithoutValidation("Authorization", authorization);
            else
                request.Headers.Authorization = AuthenticationHeaderValue.Parse(authorization);

        foreach (var header in headers)
            request.Headers.Add(header.Key, header.Value.AsEnumerable());

        var contentType = ContentType.GetOrDefault(context);
        var content = Content.GetOrDefault(context);

        if (contentType != null && content != null)
        {
            var factories = context.GetServices<IHttpContentFactory>();
            var factory = SelectContentWriter(contentType, factories);
            request.Content = factory.CreateHttpContent(content, contentType);
        }

        return request;
    }

    private IHttpContentFactory SelectContentWriter(string? contentType, IEnumerable<IHttpContentFactory> factories)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return new JsonContentFactory();

        var parsedContentType = new System.Net.Mime.ContentType(contentType);
        return factories.FirstOrDefault(httpContentFactory => httpContentFactory.SupportedContentTypes.Any(c => c == parsedContentType.MediaType)) ?? new JsonContentFactory();
    }
}