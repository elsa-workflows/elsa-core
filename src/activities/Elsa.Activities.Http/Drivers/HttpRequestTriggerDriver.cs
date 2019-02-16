using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Activities;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Http.Models;
using Elsa.Handlers;
using Elsa.Models;
using Elsa.Results;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Drivers
{
    public class HttpRequestTriggerDriver : ActivityDriver<HttpRequestTrigger>
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public HttpRequestTriggerDriver(
            IHttpContextAccessor httpContextAccessor,
            IWorkflowExpressionEvaluator expressionEvaluator)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.expressionEvaluator = expressionEvaluator;
        }

        protected override ActivityExecutionResult OnExecute(HttpRequestTrigger activity, WorkflowExecutionContext workflowContext)
        {
            return Halt();
        }

        protected override async Task<ActivityExecutionResult> OnResumeAsync(HttpRequestTrigger activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var request = httpContextAccessor.HttpContext.Request;
            var model = new HttpRequestModel
            {
                Path = new Uri(request.Path.ToString(), UriKind.Relative),
                QueryString = request.Query.ToDictionary(x => x.Key, x => x.Value),
                Headers = request.Headers.ToDictionary(x => x.Key, x => x.Value),
                Method = request.Method
            };

            if (activity.ReadContent)
            {
                if (request.HasFormContentType)
                {
                    model.Form = (await request.ReadFormAsync(cancellationToken)).ToDictionary(x => x.Key, x => x.Value);
                }
                else
                {
                    model.Content = await request.ReadBodyAsync();
                }
            }

            workflowContext.CurrentScope.LastResult = model;

            return Endpoint("Done");
        }
    }
}