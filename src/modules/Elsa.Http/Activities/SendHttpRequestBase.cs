using System.Net;
using System.Net.Http.Headers;
using Elsa.Extensions;
using Elsa.Http.ContentWriters;
using Elsa.Http.UIHints;
using Elsa.Resilience;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.UIHints;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;
using Polly;

namespace Elsa.Http;

/// <summary>
/// Base class for activities that send HTTP requests.
/// </summary>
[Output(IsSerializable = false)]
[ResilienceCategory("HTTP")]
public abstract class SendHttpRequestBase(string? source = null, int? line = null) : Activity<HttpResponseMessage>(source, line), IResilientActivity
{
    /// <summary>
    /// The URL to send the request to.
    /// </summary>
    [Input(Order = 0)] public Input<Uri?> Url { get; set; } = null!;

    /// <summary>
    /// The HTTP method to use when sending the request.
    /// </summary>
    [Input(
        Description = "The HTTP method to use when sending the request.",
        Options = new[]
        {
            "GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS", "HEAD"
        },
        DefaultValue = "GET",
        UIHint = InputUIHints.DropDown,
        Order = 1
    )]
    public Input<string> Method { get; set; } = new("GET");

    /// <summary>
    /// The content to send with the request. Can be a string, an object, a byte array or a stream.
    /// </summary>
    [Input(
        Description = "The content to send with the request. Can be a string, an object, a byte array or a stream.",
        Order = 2
        )]
    public Input<object?> Content { get; set; } = null!;

    /// <summary>
    /// The content type to use when sending the request.
    /// </summary>
    [Input(
        Description = "The content type to use when sending the request.",
        UIHandler = typeof(HttpContentTypeOptionsProvider),
        UIHint = InputUIHints.DropDown,
        Order = 3
    )]
    public Input<string?> ContentType { get; set; } = null!;

    /// <summary>
    /// The Authorization header value to send with the request.
    /// </summary>
    /// <example>Bearer {some-access-token}</example>
    [Input(
        Description = "The Authorization header value to send with the request. For example: Bearer {some-access-token}",
        Category = "Security",
        CanContainSecrets = true,
        Order = 4
    )]
    public Input<string?> Authorization { get; set; } = null!;

    /// <summary>
    /// A value that allows to add the Authorization header without validation.
    /// </summary>
    [Input(
        Description = "A value that allows to add the Authorization header without validation.", 
        Category = "Security",
        Order = 5
    )]
    public Input<bool> DisableAuthorizationHeaderValidation { get; set; } = null!;

    /// <summary>
    /// The headers to send along with the request.
    /// </summary>
    [Input(
        Description = "The headers to send along with the request.",
        UIHint = InputUIHints.JsonEditor,
        Category = "Advanced",
        Order = 6
    )]
    public Input<HttpHeaders?> RequestHeaders { get; set; } = new(new HttpHeaders());

    /// <summary>
    /// Indicates whether resiliency mechanisms should be enabled for the HTTP request.
    /// </summary>
    public Input<bool> EnableResiliency { get; set; } = null!;

    /// <summary>
    /// The HTTP response status code
    /// </summary>
    [Output(Description = "The HTTP response status code")]
    public Output<int> StatusCode { get; set; } = null!;

    /// <summary>
    /// The parsed content, if any.
    /// </summary>
    [Output(Description = "The parsed content, if any.")]
    public Output<object?> ParsedContent { get; set; } = null!;

    /// <summary>
    /// The response headers that were received.
    /// </summary>
    [Output(Description = "The response headers that were received.")]
    public Output<HttpHeaders?> ResponseHeaders { get; set; } = null!;

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
        var logger = (ILogger)context.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType()));
        var httpClientFactory = context.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(nameof(SendHttpRequestBase));
        var cancellationToken = context.CancellationToken;
        var resiliencyEnabled = EnableResiliency.GetOrDefault(context, () => false);

        try
        {
            var response = await SendRequestAsync(context);
            var parsedContent = await ParseContentAsync(context, response);
            var statusCode = (int)response.StatusCode;
            var responseHeaders = new HttpHeaders(response.Headers);

            context.Set(Result, response);
            context.Set(ParsedContent, parsedContent);
            context.Set(StatusCode, statusCode);
            context.Set(ResponseHeaders, responseHeaders);

            await HandleResponseAsync(context, response);
        }
        catch (HttpRequestException e)
        {
            logger.LogWarning(e, "An error occurred while sending an HTTP request");
            context.AddExecutionLogEntry("Error", e.Message, payload: new
            {
                e.StackTrace
            });
            context.JournalData.Add("Error", e.Message);
            await HandleRequestExceptionAsync(context, e);
        }
        catch (TaskCanceledException e)
        {
            logger.LogWarning(e, "An error occurred while sending an HTTP request");
            context.AddExecutionLogEntry("Error", e.Message, payload: new
            {
                e.StackTrace
            });
            context.JournalData.Add("Cancelled", true);
            await HandleTaskCanceledExceptionAsync(context, e);
        }

        return;

        async Task<HttpResponseMessage> SendRequestAsync(ActivityExecutionContext activityExecutionContext)
        {
            // Keep this for backward compatibility.
            if (resiliencyEnabled)
            {
                var pipeline = BuildResiliencyPipeline(context);
                return await pipeline.ExecuteAsync(async ct => await SendRequestAsyncCore(ct), cancellationToken);
            }
            
            var resilienceService = activityExecutionContext.GetRequiredService<IResilientActivityInvoker>();
            return await resilienceService.InvokeAsync(this, activityExecutionContext, async () => await SendRequestAsyncCore(cancellationToken), cancellationToken);
        }

        async Task<HttpResponseMessage> SendRequestAsyncCore(CancellationToken ct = default)
        {
            var request = PrepareRequest(context);
            return await httpClient.SendAsync(request, ct);
        }
    }

    private async Task<object?> ParseContentAsync(ActivityExecutionContext context, HttpResponseMessage httpResponse)
    {
        var httpContent = httpResponse.Content;
        if (!HasContent(httpContent))
            return null;

        var cancellationToken = context.CancellationToken;
        var targetType = ParsedContent.GetTargetType(context);
        var contentStream = await httpContent.ReadAsStreamAsync(cancellationToken);
        var responseHeaders = httpResponse.Headers;
        var contentHeaders = httpContent.Headers;
        var contentType = contentHeaders.ContentType?.MediaType ?? "application/octet-stream";

        targetType ??= contentType switch
        {
            "application/json" => typeof(object),
            _ => typeof(string)
        };

        var contentHeadersDictionary = contentHeaders.ToDictionary(x => x.Key, x => x.Value.Cast<string?>().ToArray(), StringComparer.OrdinalIgnoreCase);
        var responseHeadersDictionary = responseHeaders.ToDictionary(x => x.Key, x => x.Value.Cast<string?>().ToArray(), StringComparer.OrdinalIgnoreCase);
        var headersDictionary = contentHeadersDictionary.Concat(responseHeadersDictionary).ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
        return await context.ParseContentAsync(contentStream, contentType, targetType, headersDictionary, cancellationToken);
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

    private ResiliencePipeline<HttpResponseMessage> BuildResiliencyPipeline(ActivityExecutionContext context)
    {
        // Docs: https://www.pollydocs.org/strategies/retry
        var pipelineBuilder = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new()
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<TimeoutException>() // Specific timeout exception
                    .Handle<HttpRequestException>() // Any HTTP exception
                    .HandleResult(response => IsTransientStatusCode(response.StatusCode)),
                MaxRetryAttempts = 8,
                UseJitter = false, // If enabled, adds a random value between -25% and +25% of the calculated Delay, except if BackoffType is Exponential, where a DecorrelatedJitterBackoffV2 formula is used for jitter calculation. That formula is based on Polly.Contrib.WaitAndRetry.
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential // Delay * 2^AttemptNumber, e.g. [ 2s, 4s, 8s, 16s ]. Total secs: 2 + 4 + 8 + 16 = 30
                // If BackoffType is Exponential, then the calculated Delay is multiplied by a random value between -25% and +25% of the calculated Delay, except if BackoffType is Exponential, where a DecorrelatedJitterBackoffV2 formula is used for jitter calculation. That formula is based on Polly.Contrib.WaitAndRetry.
            });

        return pipelineBuilder.Build();
    }

    // Helper method to identify transient status codes.
    private static bool IsTransientStatusCode(HttpStatusCode? statusCode)
    {
        if (statusCode is null)
        {
            // No status code -> Assume network failure, worth retrying.
            return true;
        }

        return statusCode.Value switch
        {
            HttpStatusCode.RequestTimeout => true, // 408
            HttpStatusCode.TooManyRequests => true, // 429 (if no Retry-After header is respected)
            HttpStatusCode.InternalServerError => true, // 500
            HttpStatusCode.BadGateway => true, // 502
            HttpStatusCode.ServiceUnavailable => true, // 503
            HttpStatusCode.GatewayTimeout => true, // 504
            HttpStatusCode.Conflict => true, // 409 - Can be transient in concurrency cases
            _ => false // Other errors are not transient
        };
    }
}