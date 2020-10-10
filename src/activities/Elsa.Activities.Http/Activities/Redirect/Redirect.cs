using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Results;
using Elsa.ActivityResults;
using Elsa.Attributes;
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
        private readonly IHttpContextAccessor _httpContextAccessor;

        public Redirect(IHttpContextAccessor httpContextAccessor, IStringLocalizer<Redirect> localizer)
        {
            T = localizer;
            _httpContextAccessor = httpContextAccessor;
        }
        
        private IStringLocalizer<Redirect> T { get; }

        [ActivityProperty(Hint = "The URL to redirect to (HTTP 302).")]
        public Uri Location { get; set; }
        
        [ActivityProperty(Hint = "Whether or not the redirect is permanent (HTTP 301).")]
        public bool Permanent { get; set; }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var response = _httpContextAccessor.HttpContext.Response;

            if (response.HasStarted)
                return Fault(T["Response has already started"]);
            
            return new RedirectResult(_httpContextAccessor, Location, Permanent);
        }
    }
}