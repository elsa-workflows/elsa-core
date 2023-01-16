using System.Net.Http.Headers;
using Elsa.Extensions;
using Elsa.Http.ContentWriters;
using Elsa.Http.Services;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Models;
using Microsoft.AspNetCore.Http;
using HttpRequestHeaders = Elsa.Http.Models.HttpRequestHeaders;

namespace Elsa.Http;

[Activity("Elsa", "HTTP", "Send Http Request.", DisplayName = "HTTP Request", Kind = ActivityKind.Task)]
public class SendHttpRequest : Activity<HttpResponse>
{
    [Input] public Input<Uri?> Url { get; set; } = default!;

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
    /// The parsed content, if any. The type of the value is whatever was specified via the <see cref="TargetType"/> property.
    /// </summary>
    [Output(Description = "The parsed content, if any. The type of the value is whatever was specified via the TargetType property.")]
    public Output<object?> ParsedContent { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var request = PrepareRequest(context);
        var httpClientFactory = context.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(nameof(SendHttpRequest));
        var cancellationToken = context.CancellationToken;
        var response = await httpClient.SendAsync(request, cancellationToken);
        var parsedContent = await ParseContentAsync(context, response.Content);

        context.Set(Result, response);
        context.Set(ParsedContent, parsedContent);
    }

    private async Task<object?> ParseContentAsync(ActivityExecutionContext context, HttpContent httpContent)
    {
        if (!HasContent(httpContent))
            return null;

        var cancellationToken = context.CancellationToken;
        var targetType = GetTargetType(context);
        var contentStream = await httpContent.ReadAsStreamAsync(cancellationToken);
        var contentType = httpContent.Headers.ContentType?.MediaType!;
        var parsers = context.GetServices<IHttpContentParser>().OrderByDescending(x => x.Priority).ToList();
        var contentParser = parsers.First(x => x.GetSupportsContentType(contentType));

        return await contentParser.ReadAsync(contentStream, targetType, cancellationToken);
    }
    
    // Get the target type of the response based on the specified variable type to capture the content, if any.
    private Type? GetTargetType(ActivityExecutionContext context)
    {
        var parsedContentBlock = ParsedContent.MemoryBlockReference() is Variable parsedContentVariable
            ? context.WorkflowExecutionContext.MemoryRegister.TryGetBlock(parsedContentVariable.Id, out var block)
                ? block
                : default
            : default;

        var parsedContentVariableType = (parsedContentBlock?.Metadata as VariableBlockMetadata)?.Variable.GetType();
        return parsedContentVariableType?.GenericTypeArguments.FirstOrDefault();
    }

    private static bool HasContent(HttpContent httpContent) => httpContent.Headers.ContentLength > 0;

    private HttpRequestMessage PrepareRequest(ActivityExecutionContext context)
    {
        var method = Method.TryGet(context) ?? "GET";
        var url = Url.Get(context);
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        var headers = RequestHeaders.TryGet(context) ?? new HttpRequestHeaders();
        var authorization = Authorization.TryGet(context);

        if (!string.IsNullOrWhiteSpace(authorization))
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(authorization);

        foreach (var header in headers)
            request.Headers.Add(header.Key, header.Value.AsEnumerable());

        var contentType = ContentType.TryGet(context);
        var contentWriters = context.GetServices<IHttpContentWriter>();
        var contentWriter = SelectContentWriter(contentType, contentWriters);
        var content = Content.TryGet(context);
        request.Content = contentWriter.GetContent(content, contentType);

        return request;
    }

    private IHttpContentWriter SelectContentWriter(string? contentType, IEnumerable<IHttpContentWriter> requestContentWriters) =>
        string.IsNullOrWhiteSpace(contentType) ? new StringHttpContentWriter() : requestContentWriters.First(w => w.SupportsContentType(contentType));
}