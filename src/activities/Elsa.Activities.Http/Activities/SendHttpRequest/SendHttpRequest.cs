using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;
using HttpRequestHeaders = Elsa.Activities.Http.Models.HttpRequestHeaders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    [Action(
        Category = "HTTP",
        DisplayName = "Send HTTP Request",
        Description = "Send an HTTP request.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class SendHttpRequest : Activity
    {
        private readonly HttpClient _httpClient;
        private readonly IEnumerable<IHttpResponseBodyParser> _parsers;

        public SendHttpRequest(
            IHttpClientFactory httpClientFactory,
            IEnumerable<IHttpResponseBodyParser> parsers)
        {
            _httpClient = httpClientFactory.CreateClient(nameof(SendHttpRequest));
            _parsers = parsers;
        }

        /// <summary>
        /// The URL to invoke. 
        /// </summary>
        [ActivityProperty(Hint = "The URL to send the HTTP request to.")]
        public PathString? Url { get; set; }

        /// <summary>
        /// The HTTP method to use.
        /// </summary>
        [ActivityProperty(
            Type = ActivityPropertyTypes.Select,
            Hint = "The HTTP method to use when making the request."
        )]
        [SelectOptions("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS", "HEAD")]
        public string? Method { get; set; }

        /// <summary>
        /// The body to send along with the request.
        /// </summary>
        [ActivityProperty(Hint = "The HTTP content to send along with the request.")]
        [WorkflowExpressionOptions(Multiline = true)]
        public string? Content { get; set; }

        /// <summary>
        /// The Content Type header to send along with the request body.
        /// </summary>
        [ActivityProperty(
            Type = ActivityPropertyTypes.Select,
            Hint = "The content type to send with the request (if applicable)."
        )]
        [SelectOptions("text/plain", "text/html", "application/json", "application/xml")]
        public string? ContentType { get; set; }

        [ActivityProperty(Hint = "The Authorization header value to send.")]
        public string? Authorization { get; set; }

        /// <summary>
        /// The headers to send along with the request.
        /// </summary>
        [ActivityProperty(Hint = "The headers to send along with the request.")]
        [WorkflowExpressionOptions(Multiline = true)]
        public HttpRequestHeaders RequestHeaders { get; set; } = new HttpRequestHeaders();

        [ActivityProperty(Hint = "Check to read the content of the response.")]
        public bool ReadContent { get; set; }

        /// <summary>
        /// A list of HTTP status codes this activity can handle.
        /// </summary>
        [ActivityProperty(
            Type = ActivityPropertyTypes.List,
            Hint = "A list of possible HTTP status codes to handle, comma-separated. Example: 200, 400, 404"
        )]
        public ICollection<int> SupportedStatusCodes { get; set; } = new HashSet<int>(new[] { 200 });

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var request = CreateRequest();
            var cancellationToken = context.CancellationToken;
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var hasContent = response.Content != null;
            var contentType = response.Content?.Headers.ContentType.MediaType;

            var responseModel = new HttpResponseModel
            {
                StatusCode = response.StatusCode,
                Headers = new Dictionary<string, string[]>(
                    response.Headers.ToDictionary(x => x.Key, x => x.Value.ToArray())
                )
            };

            if (hasContent && ReadContent)
            {
                var formatter = SelectContentParser(contentType!);
                responseModel.Content = await formatter.ParseAsync(response, cancellationToken);
            }

            var statusCode = (int)response.StatusCode;
            var statusOutcome = statusCode.ToString();
            var isSupportedStatusCode = SupportedStatusCodes.Contains(statusCode);
            var outcomes = new List<string> { OutcomeNames.Done, statusOutcome };

            if (!isSupportedStatusCode)
                outcomes.Add("UnSupportedStatusCode");

            return Combine(Outcomes(outcomes), Output(responseModel));
        }

        private IHttpResponseBodyParser SelectContentParser(string contentType)
        {
            var formatters = _parsers.OrderByDescending(x => x.Priority).ToList();
            return formatters.FirstOrDefault(
                x => x.SupportedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase)
            ) ?? formatters.Last();
        }

        private HttpRequestMessage CreateRequest()
        {
            var method = Method ?? HttpMethods.Get;
            var methodSupportsBody = GetMethodSupportsBody(method);
            var url = Url;
            var request = new HttpRequestMessage(new HttpMethod(Method), url);
            var authorizationHeaderValue = Authorization;
            var requestHeaders = new HeaderDictionary(RequestHeaders);

            if (methodSupportsBody)
            {
                var body = Content;
                var contentType = ContentType;

                if (!string.IsNullOrWhiteSpace(body))
                    request.Content = new StringContent(body, Encoding.UTF8, contentType);
            }

            if (!string.IsNullOrWhiteSpace(authorizationHeaderValue))
                request.Headers.Authorization = AuthenticationHeaderValue.Parse(authorizationHeaderValue);

            foreach (var header in requestHeaders)
                request.Headers.Add(header.Key, header.Value.AsEnumerable());

            return request;
        }

        private static bool GetMethodSupportsBody(string method)
        {
            var methods = new[] { "POST", "PUT", "PATCH", "DELETE" };
            return methods.Contains(method, StringComparer.InvariantCultureIgnoreCase);
        }
    }
}