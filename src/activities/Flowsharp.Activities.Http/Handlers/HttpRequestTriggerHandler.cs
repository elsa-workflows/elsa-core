using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities.Http.Activities;
using Flowsharp.Handlers;
using Flowsharp.Models;
using Flowsharp.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;

namespace Flowsharp.Activities.Http.Handlers
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

        protected override ActivityExecutionResult OnExecute(HttpRequestTrigger activity, WorkflowExecutionContext workflowContext)
        {
            var request = httpContextAccessor.HttpContext.Request;
            var requestData = new
            {
                Path = request.Path
            };
            workflowContext.CurrentScope.LastResult = requestData;
            return TriggerEndpoint("Done");
        }
    }
}