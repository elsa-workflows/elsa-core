using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Activities;
using Elsa.Expressions;
using Elsa.Handlers;
using Elsa.Models;
using Elsa.Results;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Drivers
{
    public class HttpResponseActionDriver : ActivityDriver<HttpResponseAction>
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;
        private readonly IHttpContextAccessor httpContextAccessor;

        public HttpResponseActionDriver(IWorkflowExpressionEvaluator expressionEvaluator, IHttpContextAccessor httpContextAccessor)
        {
            this.expressionEvaluator = expressionEvaluator;
            this.httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(HttpResponseAction activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var response = httpContextAccessor.HttpContext.Response;

            response.StatusCode = (int) activity.StatusCode;
            response.ContentType = await expressionEvaluator.EvaluateAsync(activity.ContentType, workflowContext, cancellationToken);

            var headersText = await expressionEvaluator.EvaluateAsync(activity.ResponseHeaders, workflowContext, cancellationToken);

            if (headersText != null)
            {
                var headersQuery =
                    from line in Regex.Split(headersText, "\\n", RegexOptions.Multiline)
                    let pair = line.Split(':', '=')
                    select new KeyValuePair<string, string>(pair[0], pair[1]);

                foreach (var header in headersQuery)
                {
                    var headerValueExpression = new WorkflowExpression<string>(activity.ResponseHeaders.Syntax, header.Value);
                    response.Headers[header.Key] = await expressionEvaluator.EvaluateAsync(headerValueExpression, workflowContext, cancellationToken);
                }
            }

            var bodyText = await expressionEvaluator.EvaluateAsync(activity.Body, workflowContext, cancellationToken);

            if (!string.IsNullOrWhiteSpace(bodyText))
            {
                await response.WriteAsync(bodyText, cancellationToken);
            }

            return Endpoint("Done");
        }
    }
}