using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Http.ContentWriters;
using Elsa.Http.UIHints;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.UIHints;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Http;

/// <summary>
/// An activity that downloads a file from a given URL.
/// </summary>
[Activity("Elsa", "HTTP", "Downloads a file from a given URL.", DisplayName = "Download File", Kind = ActivityKind.Task)]
[Output(IsSerializable = false)]
public class DownloadHttpFile : Activity<HttpFile>, IActivityPropertyDefaultValueProvider
{
    /// <inheritdoc />
    public DownloadHttpFile([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <summary>
    /// The URL to download the file from.
    /// </summary>
    [Input(DisplayName = "URL", Description = "The URL to download the file from.")]
    public Input<Uri?> Url { get; set; } = null!;

    /// <summary>
    /// The HTTP method to use when sending the request.
    /// </summary>
    [Input(
        Description = "The HTTP method to use when sending the request.",
        Options = new[]
        {
            "GET", "POST", "PUT"
        },
        DefaultValue = "GET",
        UIHint = InputUIHints.DropDown
    )]
    public Input<string> Method { get; set; } = new("GET");

    /// <summary>
    /// A list of expected status codes to handle.
    /// </summary>
    [Input(
        Description = "A list of expected status codes to handle.",
        UIHint = InputUIHints.MultiText,
        DefaultValueProvider = typeof(FlowSendHttpRequest)
    )]
    public Input<ICollection<int>> ExpectedStatusCodes { get; set; } = null!;

    /// <summary>
    /// The content to send with the request. Can be a string, an object, a byte array or a stream.
    /// </summary>
    [Input(Name = "Content", Description = "The content to send with the request. Can be a string, an object, a byte array or a stream.")]
    public Input<object?> RequestContent { get; set; } = null!;

    /// <summary>
    /// The content type to use when sending the request.
    /// </summary>
    [Input(
        DisplayName = "Content Type",
        Description = "The content type to use when sending the request.",
        UIHandler = typeof(HttpContentTypeOptionsProvider),
        UIHint = InputUIHints.DropDown
    )]
    public Input<string?> RequestContentType { get; set; } = null!;

    /// <summary>
    /// The Authorization header value to send with the request.
    /// </summary>
    /// <example>Bearer {some-access-token}</example>
    [Input(Description = "The Authorization header value to send with the request. For example: Bearer {some-access-token}", Category = "Security")]
    public Input<string?> Authorization { get; set; } = null!;

    /// <summary>
    /// A value that allows to add the Authorization header without validation.
    /// </summary>
    [Input(Description = "A value that allows to add the Authorization header without validation.", Category = "Security")]
    public Input<bool> DisableAuthorizationHeaderValidation { get; set; } = null!;

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
    /// The HTTP response.
    /// </summary>
    [Output(IsSerializable = false)]
    public Output<HttpResponseMessage> Response { get; set; } = null!;

    /// <summary>
    /// The HTTP response status code
    /// </summary>
    [Output(Description = "The HTTP response status code")]
    public Output<int> StatusCode { get; set; } = null!;

    /// <summary>
    /// The downloaded content stream, if any.
    /// </summary>
    [Output(Description = "The downloaded content stream, if any.", IsSerializable = false)]
    public Output<Stream?> ResponseContentStream { get; set; } = null!;

    /// <summary>
    /// The downloaded content bytes, if any.
    /// </summary>
    [Output(Description = "The downloaded content bytes, if any.", IsSerializable = false)]
    public Output<byte[]?> ResponseContentBytes { get; set; } = null!;

    /// <summary>
    /// The response headers that were received.
    /// </summary>
    [Output(Description = "The response headers that were received.")]
    public Output<HttpHeaders?> ResponseHeaders { get; set; } = null!;

    /// <summary>
    /// The response content headers that were received.
    /// </summary>
    [Output(DisplayName = "Content Headers", Description = "The response content headers that were received.")]
    public Output<HttpHeaders?> ResponseContentHeaders { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        await TrySendAsync(context);
    }

    private async Task TrySendAsync(ActivityExecutionContext context)
    {
        var request = PrepareRequest(context);
        var logger = (ILogger)context.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType()));
        var httpClientFactory = context.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(nameof(SendHttpRequestBase));
        var cancellationToken = context.CancellationToken;

        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);
            var file = await GetFileFromResponse(context, response, request);
            var statusCode = (int)response.StatusCode;
            var responseHeaders = new HttpHeaders(response.Headers);
            var responseContentHeaders = new HttpHeaders(response.Content.Headers);

            context.Set(Response, response);
            context.Set(ResponseContentStream, file?.Stream);
            context.Set(Result, file);
            context.Set(StatusCode, statusCode);
            context.Set(ResponseHeaders, responseHeaders);
            context.Set(ResponseContentHeaders, responseContentHeaders);
            if (ResponseContentBytes.HasTarget(context)) context.Set(ResponseContentBytes, file?.GetBytes());

            await HandleResponseAsync(context, response);
        }
        catch (HttpRequestException e)
        {
            logger.LogWarning(e, "An error occurred while sending an HTTP request");
            context.AddExecutionLogEntry("Error", e.Message, payload: new
            {
                StackTrace = e.StackTrace
            });
            context.JournalData.Add("Error", e.Message);
            await HandleRequestExceptionAsync(context, e);
        }
        catch (TaskCanceledException e)
        {
            logger.LogWarning(e, "An error occurred while sending an HTTP request");
            context.AddExecutionLogEntry("Error", e.Message, payload: new
            {
                StackTrace = e.StackTrace
            });
            context.JournalData.Add("Cancelled", true);
            await HandleTaskCanceledExceptionAsync(context, e);
        }
    }

    /// <summary>
    /// Handles the response.
    /// </summary>
    private async Task HandleResponseAsync(ActivityExecutionContext context, HttpResponseMessage response)
    {
        var expectedStatusCodes = ExpectedStatusCodes.GetOrDefault(context) ?? new List<int>(0);
        var statusCode = (int)response.StatusCode;
        var hasMatchingStatusCode = expectedStatusCodes.Contains(statusCode);
        var outcome = expectedStatusCodes.Any() ? hasMatchingStatusCode ? statusCode.ToString() : "Unmatched status code" : null;
        var outcomes = new List<string>();

        if (outcome != null)
            outcomes.Add(outcome);

        outcomes.Add("Done");
        await context.CompleteActivityWithOutcomesAsync(outcomes.ToArray());
    }

    /// <summary>
    /// Handles an exception that occurred while sending the request.
    /// </summary>
    private async Task HandleRequestExceptionAsync(ActivityExecutionContext context, HttpRequestException exception)
    {
        await context.CompleteActivityWithOutcomesAsync("Failed to connect");
    }

    /// <summary>
    /// Handles <see cref="TaskCanceledException"/> that occurred while sending the request.
    /// </summary>
    private async Task HandleTaskCanceledExceptionAsync(ActivityExecutionContext context, TaskCanceledException exception)
    {
        await context.CompleteActivityWithOutcomesAsync("Timeout");
    }

    private async Task<HttpFile?> GetFileFromResponse(ActivityExecutionContext context, HttpResponseMessage httpResponse, HttpRequestMessage httpRequestMessage)
    {
        var httpContent = httpResponse.Content;
        if (!HasContent(httpContent))
            return null;

        var cancellationToken = context.CancellationToken;
        var contentStream = await httpContent.ReadAsStreamAsync(cancellationToken);
        var responseHeaders = httpResponse.Headers;
        var contentHeaders = httpContent.Headers;
        var contentType = contentHeaders.ContentType?.MediaType!;
        var filename = contentHeaders.ContentDisposition?.FileName ?? httpRequestMessage.RequestUri!.Segments.LastOrDefault() ?? "file.dat";
        var eTag = responseHeaders.ETag?.Tag;

        return new HttpFile(contentStream, filename, contentType, eTag);
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

        var contentType = RequestContentType.GetOrDefault(context);
        var content = RequestContent.GetOrDefault(context);

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

    object IActivityPropertyDefaultValueProvider.GetDefaultValue(PropertyInfo property)
    {
        if (property.Name == nameof(ExpectedStatusCodes))
            return new List<int>
            {
                200
            };

        return null!;
    }
}