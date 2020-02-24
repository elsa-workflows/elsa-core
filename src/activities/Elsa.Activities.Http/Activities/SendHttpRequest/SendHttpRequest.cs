using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Services;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;
using HttpRequestHeaders = Elsa.Activities.Http.Models.HttpRequestHeaders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    [ActivityDefinition(
        Category = "HTTP",
        DisplayName = "Send HTTP Request",
        Description = "Send an HTTP request.",
        Outcomes = new[] { OutcomeNames.Done, "x => !!x.state.supportedStatusCodes ? ['UnSupportedStatusCode', ...x.state.supportedStatusCodes] : ['UnSupportedStatusCode']" }
    )]
    public class SendHttpRequest : Activity
    {
        private readonly HttpClient httpClient;
        private readonly IEnumerable<IHttpResponseBodyParser> parsers;

        public SendHttpRequest(
            IHttpClientFactory httpClientFactory,
            IEnumerable<IHttpResponseBodyParser> parsers)
        {
            httpClient = httpClientFactory.CreateClient(nameof(SendHttpRequest));
            this.parsers = parsers;
        }

        /// <summary>
        /// The URL to invoke. 
        /// </summary>
        [ActivityProperty(Hint = "The URL to send the HTTP request to.")]
        public IWorkflowExpression<PathString> Url
        {
            get => GetState<IWorkflowExpression<PathString>>();
            set => SetState(value);
        }

        /// <summary>
        /// The HTTP method to use.
        /// </summary>
        [ActivityProperty(
            Type = ActivityPropertyTypes.Select,
            Hint = "The HTTP method to use when making the request."
        )]
        [SelectOptions("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS", "HEAD")]
        public string Method
        {
            get => GetState(() => "GET");
            set => SetState(value);
        }

        /// <summary>
        /// The body to send along with the request.
        /// </summary>
        [ActivityProperty(Hint = "The HTTP content to send along with the request.")]
        [WorkflowExpressionOptions(Multiline = true)]
        public IWorkflowExpression<string>? Content
        {
            get => GetState<IWorkflowExpression<string>>();
            set => SetState(value);
        }

        /// <summary>
        /// The Content Type header to send along with the request body.
        /// </summary>
        [ActivityProperty(
            Type = ActivityPropertyTypes.Select,
            Hint = "The content type to send with the request (if applicable)."
        )]
        [SelectOptions("text/plain", "text/html", "application/json", "application/xml")]
        public IWorkflowExpression<string> ContentType
        {
            get => GetState<IWorkflowExpression<string>>();
            set => SetState(value);
        }

        [ActivityProperty(
            Hint = "The Authorization header value to send."
        )]
        public IWorkflowExpression<string> Authorization
        {
            get => GetState<IWorkflowExpression<string>>();
            set => SetState(value);
        }

        /// <summary>
        /// The headers to send along with the request.
        /// </summary>
        [ActivityProperty(Hint = "The headers to send along with the request.")]
        [WorkflowExpressionOptions(Multiline = true)]
        public IWorkflowExpression<HttpRequestHeaders>? RequestHeaders
        {
            get => GetState<IWorkflowExpression<HttpRequestHeaders>>();
            set => SetState(value);
        }

        [ActivityProperty(Hint = "Check to read the content of the response.")]
        public bool ReadContent
        {
            get => GetState(() => true);
            set => SetState(value);
        }

        /// <summary>
        /// A list of HTTP status codes this activity can handle.
        /// </summary>
        [ActivityProperty(
            Type = ActivityPropertyTypes.List,
            Hint = "A list of possible HTTP status codes to handle, comma-separated. Example: 200, 400, 404"
        )]
        public ICollection<int> SupportedStatusCodes
        {
            get => GetState(() => new HashSet<int> { 200 });
            set => SetState(new HashSet<int>(value));
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var request = await CreateRequestAsync(context, cancellationToken);
            var response = await httpClient.SendAsync(request, cancellationToken);
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
                var formatter = SelectContentParser(contentType);

                responseModel.Content = await formatter.ParseAsync(response, cancellationToken);
            }

            var statusCode = (int)response.StatusCode;
            var statusOutcome = statusCode.ToString();
            var isSupportedStatusCode = SupportedStatusCodes.Contains(statusCode);
            var outcomes = new List<string> { OutcomeNames.Done, statusOutcome };

            if (!isSupportedStatusCode)
                outcomes.Add("UnSupportedStatusCode");

            return Done(outcomes, responseModel);
        }

        private IHttpResponseBodyParser SelectContentParser(string contentType)
        {
            var formatters = parsers.OrderByDescending(x => x.Priority).ToList();
            return formatters.FirstOrDefault(
                       x => x.SupportedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase)
                   ) ?? formatters.Last();
        }

        private async Task<HttpRequestMessage> CreateRequestAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var methodSupportsBody = GetMethodSupportsBody(Method);
            var uri = await context.EvaluateAsync(Url, cancellationToken);
            var request = new HttpRequestMessage(new HttpMethod(Method), uri);
            var authorizationHeaderValue = await context.EvaluateAsync(Authorization, cancellationToken);
            var requestHeaders = await ParseRequestHeadersAsync(context, cancellationToken);

            if (methodSupportsBody)
            {
                var body = await context.EvaluateAsync(Content, cancellationToken);
                var contentType = await context.EvaluateAsync(ContentType, cancellationToken);

                if (!string.IsNullOrWhiteSpace(body))
                    request.Content = new StringContent(body, Encoding.UTF8, contentType);
            }

            if (!string.IsNullOrWhiteSpace(authorizationHeaderValue))
                request.Headers.Authorization = AuthenticationHeaderValue.Parse(authorizationHeaderValue);

            foreach (var header in requestHeaders)
                request.Headers.Add(header.Key, header.Value.AsEnumerable());

            return request;
        }

        private async Task<IHeaderDictionary> ParseRequestHeadersAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var headers = await context.EvaluateAsync(RequestHeaders, cancellationToken);
            return new HeaderDictionary(headers);
        }

        private static bool GetMethodSupportsBody(string method)
        {
            var methods = new[] { "POST", "PUT", "PATCH" };
            return methods.Contains(method, StringComparer.InvariantCultureIgnoreCase);
        }
    }
}