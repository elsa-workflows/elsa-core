using System;
using System.Collections.Generic;
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
        private readonly IEnumerable<IContentFormatter> contentFormatters;

        public HttpRequestTriggerDriver(
            IHttpContextAccessor httpContextAccessor,
            IWorkflowExpressionEvaluator expressionEvaluator,
            IEnumerable<IContentFormatter> contentFormatters)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.expressionEvaluator = expressionEvaluator;
            this.contentFormatters = contentFormatters;
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

                var formatter = SelectContentFormatter(request.ContentType);
                var content = await request.ReadBodyAsync();
                model.Content = content;
                model.FormattedContent = await formatter.FormatAsync(content, request.ContentType);
            }

            workflowContext.CurrentScope.LastResult = model;

            return Endpoint("Done");
        }

        private IContentFormatter SelectContentFormatter(string contentType)
        {
            var formatters = contentFormatters.OrderByDescending(x => x.Priority).ToList();
            return formatters.FirstOrDefault(x => x.SupportedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase)) ?? formatters.Last();
        }
    }
}