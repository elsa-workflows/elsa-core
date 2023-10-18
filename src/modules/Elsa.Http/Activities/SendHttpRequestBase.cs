using System.Net.Http.Headers;
using Elsa.Extensions;
using Elsa.Http.ActivityOptionProviders;
using Elsa.Http.ContentWriters;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using HttpRequestHeaders = Elsa.Http.Models.HttpRequestHeaders;

namespace Elsa.Http;

/// <summary>
/// Base class for activities that send HTTP requests.
/// </summary>
[Output(IsSerializable = false)]
public abstract class SendHttpRequestBase : Activity<HttpResponseMessage>
{
    /// <inheritdoc />
    protected SendHttpRequestBase(string? source = default, int? line = default) : base(source, line)
    {
    }

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
        OptionsProvider = typeof(HttpContentTypeOptionsProvider),
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

    /// <summary>
    /// Handles an exception that occurred while sending the request.
    /// </summary>
    protected abstract ValueTask HandleRequestExceptionAsync(ActivityExecutionContext context, HttpRequestException exception);

    /// <summary>
    /// Handles <see cref="TaskCanceledException"/> that occurred while sending the request.
    /// </summary>
    protected abstract ValueTask HandleTaskCanceledExceptionAsync(ActivityExecutionContext context, TaskCanceledException exception);

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

        if (!string.IsNullOrWhiteSpace(authorization))
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