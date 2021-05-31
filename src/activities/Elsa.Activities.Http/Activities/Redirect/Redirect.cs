using System;
using Elsa.Activities.Http.Results;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    [Action(
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

        [ActivityInput(Hint = "The URL to redirect to (HTTP 302).", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public Uri Location { get; set; } = default!;

        [ActivityInput(Hint = "Whether or not the redirect is permanent (HTTP 301).", SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public bool Permanent { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            var response = _httpContextAccessor.HttpContext.Response;

            if (response.HasStarted)
                return Fault(T["Response has already started"]!);

            return new RedirectResult(_httpContextAccessor, Location, Permanent);
        }
    }
}