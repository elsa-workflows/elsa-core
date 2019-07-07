using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Core.Expressions;
using Elsa.Core.Extensions;
using Elsa.Core.Services;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Activities
{
    public class HttpResponseAction : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;
        private readonly IHttpContextAccessor httpContextAccessor;

        public HttpResponseAction(IWorkflowExpressionEvaluator expressionEvaluator, IHttpContextAccessor httpContextAccessor)
        {
            this.expressionEvaluator = expressionEvaluator;
            this.httpContextAccessor = httpContextAccessor;
        }
        
        /// <summary>
        /// The HTTP status code to return.
        /// </summary>
        public HttpStatusCode StatusCode
        {
            get => GetState(() => HttpStatusCode.OK);
            set => SetState(value);
        }

        /// <summary>
        /// The body to send along with the response
        /// </summary>
        public WorkflowExpression<string> Body
        {
            get => GetState(() => new WorkflowExpression<string>(PlainTextEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        /// <summary>
        /// The Content-Type header to send along with the response.
        /// </summary>
        public WorkflowExpression<string> ContentType
        {
            get => GetState(() => new WorkflowExpression<string>(PlainTextEvaluator.SyntaxName, ""));
            set => SetState(value);
        }
        
        /// <summary>
        /// The headers to send along with the response, one header: value pair per line.
        /// </summary>
        public WorkflowExpression<string> ResponseHeaders
        {
            get => GetState(() => new WorkflowExpression<string>(PlainTextEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var response = httpContextAccessor.HttpContext.Response;

            response.StatusCode = (int) StatusCode;
            response.ContentType = await expressionEvaluator.EvaluateAsync(ContentType, workflowContext, cancellationToken);

            var headersText = await expressionEvaluator.EvaluateAsync(ResponseHeaders, workflowContext, cancellationToken);

            if (headersText != null)
            {
                var headersQuery =
                    from line in Regex.Split(headersText, "\\n", RegexOptions.Multiline)
                    let pair = line.Split(':', '=')
                    select new KeyValuePair<string, string>(pair[0], pair[1]);

                foreach (var header in headersQuery)
                {
                    var headerValueExpression = new WorkflowExpression<string>(ResponseHeaders.Syntax, header.Value);
                    response.Headers[header.Key] = await expressionEvaluator.EvaluateAsync(headerValueExpression, workflowContext, cancellationToken);
                }
            }

            var bodyText = await expressionEvaluator.EvaluateAsync(Body, workflowContext, cancellationToken);

            if (!string.IsNullOrWhiteSpace(bodyText))
            {
                await response.WriteAsync(bodyText, cancellationToken);
            }

            return Done();
        }
    }
}