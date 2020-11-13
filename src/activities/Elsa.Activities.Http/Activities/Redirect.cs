using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Activities
{
    [ActivityDefinition(
        Category = "HTTP",
        DisplayName = "Redirect",
        Description = "Write an HTTP redirect response.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class Redirect : Activity
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public Redirect(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        [ActivityProperty(Hint = "The URL to redirect to (HTTP 302).")]
        public WorkflowExpression<string> Location
        {
            get => GetState<WorkflowExpression<string>>();
            set => SetState(value);
        }
        
        [ActivityProperty(Hint = "Tick this box to indicate if the redirect is permanent (HTTP 301).")]
        public bool Permanent
        {
            get => GetState(() => false);
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var httpContext = httpContextAccessor.HttpContext;
            var response = httpContext.Response;

            if (response.HasStarted)
                return Fault("Response has already started");

            var location = await workflowContext.EvaluateAsync(Location, cancellationToken);
            response.Redirect(location, Permanent);
            httpContext.Items[WorkflowHttpResult.Instance] = WorkflowHttpResult.Instance;
            
            return Done();
        }
    }
}