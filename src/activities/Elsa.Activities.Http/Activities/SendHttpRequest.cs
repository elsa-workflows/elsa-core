using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Services;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Activities
{
    [ActivityDefinition(
        Category = "HTTP",
        DisplayName = "Send HTTP Request",
        Description = "Send an HTTP request.",
        RuntimeDescription = "x => !!x.state.url ? `Send HTTP <strong>${ x.state.method } ${ x.state.url.expression }</strong>.` : x.definition.description",
        Outcomes = "x => !!x.state.supportedStatusCodes ? x.state.supportedStatusCodes : []"
    )]
    public class SendHttpRequest : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;
        private readonly HttpClient httpClient;
        private readonly IEnumerable<IHttpResponseBodyParser> parsers;

        public SendHttpRequest(
            IWorkflowExpressionEvaluator expressionEvaluator,
            IHttpClientFactory httpClientFactory,
            IEnumerable<IHttpResponseBodyParser> parsers)
        {
            this.expressionEvaluator = expressionEvaluator;
            httpClient = httpClientFactory.CreateClient(nameof(SendHttpRequest));
            this.parsers = parsers;
        }

        /// <summary>
        /// The URL to invoke. 
        /// </summary>
        [ActivityProperty(Hint = "The URL to send the HTTP request to.")]
        public WorkflowExpression<Uri> Url
        {
            get
            {
                var url = GetState<WorkflowExpression<string>>();

                if (url != null && !string.IsNullOrWhiteSpace(url.Expression) && Uri.TryCreate(
                    url.Expression,
                    UriKind.RelativeOrAbsolute,
                    out var uri
                ))
                    return new WorkflowExpression<Uri>(url.Syntax, uri.ToString());

                return new WorkflowExpression<Uri>(LiteralEvaluator.SyntaxName, "");
            }
            set => SetState(new WorkflowExpression<string>(value.Syntax, value.Expression));
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
        [ExpressionOptions(Multiline = true)]
        public WorkflowExpression<string> Content
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
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
        public string ContentType
        {
            get => GetState<string>(() => "text/plain");
            set => SetState(value);
        }

        [ActivityProperty(
            Hint = "The Authorization header value to send."
        )]
        public WorkflowExpression<string> Authorization
        {
            get => GetState<WorkflowExpression<string>>();
            set => SetState(value);
        }

        /// <summary>
        /// The headers to send along with the request.
        /// </summary>
        [ActivityProperty(Hint = "The headers to send along with the request. One 'header: value' pair per line.")]
        public WorkflowExpression<string> RequestHeaders
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
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
        public HashSet<int> SupportedStatusCodes
        {
            get => GetState(() => new HashSet<int> { 200 });
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(
            WorkflowExecutionContext workflowContext,
            CancellationToken cancellationToken)
        {
            var request = await CreateRequestAsync(workflowContext, cancellationToken);
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

            workflowContext.SetLastResult(Output.SetVariable("Response", responseModel));

            var statusEndpoint = ((int)response.StatusCode).ToString();

            return Outcomes(new[] { OutcomeNames.Done, statusEndpoint });
        }

        private IHttpResponseBodyParser SelectContentParser(string contentType)
        {
            var formatters = parsers.OrderByDescending(x => x.Priority).ToList();
            return formatters.FirstOrDefault(
                x => x.SupportedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase)
            ) ?? formatters.Last();
        }

        private async Task<HttpRequestMessage> CreateRequestAsync(
            WorkflowExecutionContext workflowContext,
            CancellationToken cancellationToken)
        {
            var methodSupportsBody = GetMethodSupportsBody(Method);
            var uri = await expressionEvaluator.EvaluateAsync(Url, workflowContext, cancellationToken);
            var request = new HttpRequestMessage(new HttpMethod(Method), uri);

            var authorizationHeaderValue = await expressionEvaluator.EvaluateAsync(
                Authorization,
                workflowContext,
                cancellationToken
            );

            var requestHeaders = await ParseRequestHeadersAsync(workflowContext, cancellationToken);

            if (methodSupportsBody)
            {
                var body = await expressionEvaluator.EvaluateAsync(Content, workflowContext, cancellationToken);
                var contentType = ContentType;

                if (!string.IsNullOrWhiteSpace(body))
                {
                    request.Content = new StringContent(body, Encoding.UTF8, contentType);
                }
            }

            if (!string.IsNullOrWhiteSpace(authorizationHeaderValue))
            {
                request.Headers.Authorization = AuthenticationHeaderValue.Parse(authorizationHeaderValue);
            }

            foreach (var header in requestHeaders)
            {
                request.Headers.Add(header.Key, header.Value.AsEnumerable());
            }

            return request;
        }

        private async Task<IHeaderDictionary> ParseRequestHeadersAsync(
            WorkflowExecutionContext workflowContext,
            CancellationToken cancellationToken)
        {
            var headersText = await expressionEvaluator.EvaluateAsync(
                RequestHeaders,
                workflowContext,
                cancellationToken
            );
            var headers = new HeaderDictionary();

            if (!string.IsNullOrWhiteSpace(headersText))
            {
                var headersQuery =
                    from line in Regex.Split(headersText, "\\n", RegexOptions.Multiline)
                    let pair = line.Split(new[] { ':', '=' }, 2)
                    select new KeyValuePair<string, string>(pair[0], pair[1]);

                foreach (var header in headersQuery)
                {
                    var headerValueExpression = new WorkflowExpression<string>(RequestHeaders.Syntax, header.Value);
                    var headerValue = await expressionEvaluator.EvaluateAsync(
                        headerValueExpression,
                        workflowContext,
                        cancellationToken
                    );
                    headers.Add(header.Key, headerValue);
                }
            }

            return headers;
        }

        private bool GetMethodSupportsBody(string method)
        {
            var methods = new[] { "POST", "PUT", "PATCH", "DELETE" };
            return methods.Contains(method, StringComparer.InvariantCultureIgnoreCase);
        }
    }
}