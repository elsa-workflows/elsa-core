using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Results;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
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

        public Redirect(IHttpContextAccessor httpContextAccessor, IStringLocalizer<Redirect> localizer)
        {
            T = localizer;
            this.httpContextAccessor = httpContextAccessor;
        }
        
        private IStringLocalizer<Redirect> T { get; }

        [ActivityProperty(Hint = "The URL to redirect to (HTTP 302).")]
        public IWorkflowExpression<PathString> Location
        {
            get => GetState<IWorkflowExpression<PathString>>();
            set => SetState(value);
        }
        
        [ActivityProperty(Hint = "Whether or not the redirect is permanent (HTTP 301).")]
        public bool Permanent
        {
            get => GetState(() => false);
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var response = httpContextAccessor.HttpContext.Response;

            if (response.HasStarted)
                return Fault(T["Response has already started"]);

            var location = await context.EvaluateAsync(Location, cancellationToken);
            return new RedirectResult(httpContextAccessor, location, Permanent);
        }
    }
}