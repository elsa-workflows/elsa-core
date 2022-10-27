using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Elsa.Http.ContentWriters;
using Elsa.Http.Models;
using Elsa.Http.Parsers;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using HttpRequestHeaders = Elsa.Http.Models.HttpRequestHeaders;

namespace Elsa.Http;

[Activity("Elsa", "HTTP", "Send Http Request.", DisplayName = "Send HTTP Request", Kind = ActivityKind.Task)]
public class SendHttpRequest : Activity
{
    [Input] public Input<Uri?> Url { get; set; } = default!;

    [Input(
        Options = new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS", "HEAD" },
        UIHint = InputUIHints.Dropdown
    )]
    public Input<string?> Method { get; set; }

    public Input<string?> Content { get; set; } = default!;

    [Input(
        Options = new[] { "", "text/plain", "text/html", "application/json", "application/xml", "application/x-www-form-urlencoded" },
        UIHint = InputUIHints.Dropdown
    )]
    public Input<string?> ContentType { get; set; } = default!;

    [Input(Category = "Security")] public Input<string?> Authorization { get; set; } = default!;

    public Input<bool> ReadContent { get; set; } = new(false);

    [Input(
        Options = new[] { "", "JsonElement", "Plain Text" },
        UIHint = InputUIHints.Dropdown
    )]
    public Input<string?> ResponseContentParserName { get; set; } = default!;

    [Input(Category = "Security")] public Input<Dictionary<string, string>?> RequestHeaders { get; set; } = new(new HttpRequestHeaders());

    [Output] public Output<object>? ResponseContent { get; set; }

    [Output] public Output<HttpResponseModel>? Response { get; set; }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var request = PrepareRequest(context);

        var httpClientFactory = context.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(nameof(SendHttpRequest));

        var cancellationToken = context.CancellationToken;
        var response = await httpClient.SendAsync(request, cancellationToken);

        var allHeaders =
            response.Headers.ToDictionary(x => x.Key, x => x.Value.ToArray())
                .Concat(response.Content.Headers.ToDictionary(x => x.Key, x => x.Value.ToArray()));

        var responseModel = new HttpResponseModel(response.StatusCode, new Dictionary<string, string[]>())
        {
            StatusCode = response!.StatusCode,
            Headers = new Dictionary<string, string[]>(allHeaders)
        };

        context.Set(Response, responseModel);

        if (HasContent(response) && context.Get(ReadContent))
        {
            var parsers = context.GetServices<IHttpResponseContentReader>();
            var formatter = SelectContentParser(parsers.ToList(), context.Get(ResponseContentParserName), context.Get(ContentType));
            context.Set(ResponseContent, await formatter.ReadAsync(response, context, cancellationToken));
        }
    }

    private IHttpResponseContentReader SelectContentParser(List<IHttpResponseContentReader> parsers, string? parserName, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(parserName))
        {
            var simpleContentType = contentType?.Split(';').First() ?? "";
            var parser = parsers.OrderByDescending(x => x.Priority).ToList();

            return parser.FirstOrDefault(x => x.GetSupportsContentType(simpleContentType)) ?? parser.Last();
        }
        else
        {
            var parser = parsers.FirstOrDefault(x => x.Name == parserName);

            if (parser == null)
                throw new InvalidOperationException("The specified parser does not exist");

            return parser;
        }
    }

    private bool HasContent(HttpResponseMessage response) => response?.Content != null && response.Content.Headers.ContentLength > 0;

    private HttpRequestMessage PrepareRequest(ActivityExecutionContext context)
    {
        var method = context.Get(Method)!;
        var request = new HttpRequestMessage(new HttpMethod(method), context.Get(Url));

        var headers = context.Get(RequestHeaders)!;
        var requestHeaders = new HeaderDictionary(headers.ToDictionary(x => x.Key, x => new StringValues(x.Value.Split(','))));

        if (!string.IsNullOrWhiteSpace(context.Get(Authorization)))
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(context.Get(Authorization));

        foreach (var header in requestHeaders)
            request.Headers.Add(header.Key, header.Value.AsEnumerable());

        var contentType = context.Get(ContentType)!;
        var contentWriters = context.GetServices<IHttpRequestContentWriter>();
        var contentWriter = SelectContentWriter(contentType, contentWriters);
        request.Content = contentWriter.GetContent(contentType, context.Get(Content));

        return request;
    }

    private IHttpRequestContentWriter SelectContentWriter(string contentType, IEnumerable<IHttpRequestContentWriter> requestContentWriters) => 
        string.IsNullOrWhiteSpace(contentType) ? new StringHttpRequestContentWriter() : requestContentWriters.First(w => w.SupportsContentType(contentType));
}