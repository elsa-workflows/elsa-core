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
using Microsoft.Extensions.Localization;

namespace Elsa.Activities.Http.Handlers
{
    public class HttpRequestTriggerHandler : ActivityHandler<HttpRequestTrigger>
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public HttpRequestTriggerHandler(
            IStringLocalizer<HttpRequestTrigger> localizer,
            IHttpContextAccessor httpContextAccessor,
            IWorkflowExpressionEvaluator expressionEvaluator)
        {
            T = localizer;
            this.httpContextAccessor = httpContextAccessor;
            this.expressionEvaluator = expressionEvaluator;
        }

        public IStringLocalizer<HttpRequestTrigger> T { get; }
        public override bool IsTrigger => true;
        public override LocalizedString Category => T["HTTP"];
        public override LocalizedString DisplayText => T["HTTP Request"];
        public override LocalizedString Description => T["Triggers when an incoming HTTP request is received."];
        protected override LocalizedString GetEndpoint() => T["Done"];

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(HttpRequestTrigger activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
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
            return TriggerEndpoint("Done");
        }
    }
}