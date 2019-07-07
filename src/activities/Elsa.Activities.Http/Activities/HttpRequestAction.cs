using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Services;
using Elsa.Core.Expressions;
using Elsa.Core.Extensions;
using Elsa.Core.Services;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Elsa.Activities.Http.Activities
{
    public class HttpRequestAction : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;
        private readonly HttpClient httpClient;
        private readonly IEnumerable<IContentFormatter> contentFormatters;

        public HttpRequestAction(
            IWorkflowExpressionEvaluator expressionEvaluator,
            IHttpClientFactory httpClientFactory,
            IEnumerable<IContentFormatter> contentFormatters)
        {
            this.expressionEvaluator = expressionEvaluator;
            httpClient = httpClientFactory.CreateClient(nameof(HttpRequestAction));
            this.contentFormatters = contentFormatters;
        }

        /// <summary>
        /// The URL to invoke. 
        /// </summary>
        public WorkflowExpression<Uri> Url
        {
            get
            {
                var url = GetState<string>();

                if (!string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
                    return new WorkflowExpression<Uri>(PlainTextEvaluator.SyntaxName, uri.ToString());

                return new WorkflowExpression<Uri>(PlainTextEvaluator.SyntaxName, "");
            }
            set => SetState(value.ToString());
        }

        /// <summary>
        /// The HTTP method to use.
        /// </summary>
        public string Method
        {
            get => GetState(() => "GET");
            set => SetState(value);
        }

        /// <summary>
        /// The body to send along with the request.
        /// </summary>
        public WorkflowExpression<string> Body
        {
            get => GetState(() => new WorkflowExpression<string>(PlainTextEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        /// <summary>
        /// The Content Type header to send along with the request body.
        /// </summary>
        public WorkflowExpression<string> ContentType
        {
            get => GetState(() => new WorkflowExpression<string>(PlainTextEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        /// <summary>
        /// The headers to send along with the request.
        /// </summary>
        public WorkflowExpression<string> RequestHeaders
        {
            get => GetState(() => new WorkflowExpression<string>(PlainTextEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        /// <summary>
        /// A list of HTTP status codes this activity can handle.
        /// </summary>
        public HashSet<int> SupportedStatusCodes
        {
            get => GetState(() => new HashSet<int> { 200 });
            set => SetState(value);
        }


        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext workflowContext,
            CancellationToken cancellationToken)
        {
            var request = await CreateRequestAsync(workflowContext, cancellationToken);
            var response = await httpClient.SendAsync(request, cancellationToken);
            var content = response.Content != null ? await response.Content.ReadAsStringAsync() : default(string);
            var contentType = response.Content?.Headers.ContentType.MediaType;
            var formatter = SelectContentFormatter(contentType);

            var responseModel = new HttpResponseModel
            {
                StatusCode = response.StatusCode,
                Headers = new HeaderDictionary(response.Headers.ToDictionary(x => x.Key, x => new StringValues(x.Value.ToArray()))),
                Content = content,
                FormattedContent = await formatter.ParseAsync(content, contentType)
            };

            workflowContext.SetLastResult(responseModel);
            var statusEndpoint = ((int) response.StatusCode).ToString();

            return Outcomes(new[] { OutcomeNames.Done, statusEndpoint });
        }

        private IContentFormatter SelectContentFormatter(string contentType)
        {
            var formatters = contentFormatters.OrderByDescending(x => x.Priority).ToList();
            return formatters.FirstOrDefault(x => x.SupportedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase)) ?? formatters.Last();
        }

        private async Task<HttpRequestMessage> CreateRequestAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var methodSupportsBody = GetMethodSupportsBody(Method);
            var uri = await expressionEvaluator.EvaluateAsync(Url, workflowContext, cancellationToken);
            var request = new HttpRequestMessage(new HttpMethod(Method), uri);
            var requestHeaders = await ParseRequestHeadersAsync(workflowContext, cancellationToken);

            if (methodSupportsBody)
            {
                var body = await expressionEvaluator.EvaluateAsync(Body, workflowContext, cancellationToken);
                var contentType = await expressionEvaluator.EvaluateAsync(ContentType, workflowContext, cancellationToken);

                if (!string.IsNullOrWhiteSpace(body))
                {
                    request.Content = new StringContent(body, Encoding.UTF8, contentType);
                }
            }

            foreach (var header in requestHeaders)
            {
                request.Headers.Add(header.Key, header.Value.AsEnumerable());
            }

            return request;
        }

        private async Task<IHeaderDictionary> ParseRequestHeadersAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var headersText = await expressionEvaluator.EvaluateAsync(RequestHeaders, workflowContext, cancellationToken);
            var headers = new HeaderDictionary();

            if (headersText != null)
            {
                var headersQuery =
                    from line in Regex.Split(headersText, "\\n", RegexOptions.Multiline)
                    let pair = line.Split(':', '=')
                    select new KeyValuePair<string, string>(pair[0], pair[1]);

                foreach (var header in headersQuery)
                {
                    var headerValueExpression = new WorkflowExpression<string>(RequestHeaders.Syntax, header.Value);
                    var headerValue = await expressionEvaluator.EvaluateAsync(headerValueExpression, workflowContext, cancellationToken);
                    headers.Add(header.Key, headerValue);
                }
            }

            return headers;
        }

        private bool GetMethodSupportsBody(string method)
        {
            var methods = new[] { "POST", "PUT", "PATCH" };
            return methods.Contains(method, StringComparer.InvariantCultureIgnoreCase);
        }
    }
}