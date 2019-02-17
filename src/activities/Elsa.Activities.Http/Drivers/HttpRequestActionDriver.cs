using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Activities;
using Elsa.Activities.Http.Models;
using Elsa.Expressions;
using Elsa.Handlers;
using Elsa.Models;
using Elsa.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Elsa.Activities.Http.Drivers
{
    public class HttpRequestActionDriver : ActivityDriver<HttpRequestAction>
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;
        private readonly HttpClient httpClient;
        private readonly IEnumerable<IContentFormatter> contentFormatters;

        public HttpRequestActionDriver(
            IWorkflowExpressionEvaluator expressionEvaluator,
            IHttpClientFactory httpClientFactory,
            IEnumerable<IContentFormatter> contentFormatters)
        {
            this.expressionEvaluator = expressionEvaluator;
            httpClient = httpClientFactory.CreateClient(nameof(HttpRequestActionDriver));
            this.contentFormatters = contentFormatters;
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(HttpRequestAction activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var request = await CreateRequestAsync(activity, workflowContext, cancellationToken);
            var response = await httpClient.SendAsync(request, cancellationToken);
            var content = response.Content != null ? await response.Content.ReadAsStringAsync() : default(string);
            var contentType = response.Content?.Headers.ContentType.MediaType;
            var formatter = SelectContentFormatter(contentType);

            var responseModel = new HttpResponseModel
            {
                StatusCode = response.StatusCode,
                Headers = new HeaderDictionary(response.Headers.ToDictionary(x => x.Key, x => new StringValues(x.Value.ToArray()))),
                Content = content,
                FormattedContent = await formatter.FormatAsync(content, contentType)
            };

            workflowContext.SetLastResult(responseModel);
            var statusEndpoint = ((int) response.StatusCode).ToString();

            return Endpoints(new[] { "Done", statusEndpoint });
        }

        private IContentFormatter SelectContentFormatter(string contentType)
        {
            var formatters = contentFormatters.OrderByDescending(x => x.Priority).ToList();
            return formatters.FirstOrDefault(x => x.SupportedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase)) ?? formatters.Last();
        }

        private async Task<HttpRequestMessage> CreateRequestAsync(HttpRequestAction activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var methodSupportsBody = GetMethodSupportsBody(activity.Method);
            var uri = await expressionEvaluator.EvaluateAsync(activity.Url, workflowContext, cancellationToken);
            var request = new HttpRequestMessage(new HttpMethod(activity.Method), uri);
            var requestHeaders = await ParseRequestHeadersAsync(activity, workflowContext, cancellationToken);

            if (methodSupportsBody)
            {
                var body = await expressionEvaluator.EvaluateAsync(activity.Body, workflowContext, cancellationToken);
                var contentType = await expressionEvaluator.EvaluateAsync(activity.ContentType, workflowContext, cancellationToken);

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

        private async Task<IHeaderDictionary> ParseRequestHeadersAsync(HttpRequestAction activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var headersText = await expressionEvaluator.EvaluateAsync(activity.RequestHeaders, workflowContext, cancellationToken);
            var headers = new HeaderDictionary();

            if (headersText != null)
            {
                var headersQuery =
                    from line in Regex.Split(headersText, "\\n", RegexOptions.Multiline)
                    let pair = line.Split(':', '=')
                    select new KeyValuePair<string, string>(pair[0], pair[1]);

                foreach (var header in headersQuery)
                {
                    var headerValueExpression = new WorkflowExpression<string>(activity.RequestHeaders.Syntax, header.Value);
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