using System;
using System.Collections.Generic;
using Elsa.Activities.Http.Models;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    [Trigger(
        Category = "HTTP",
        DisplayName = "HTTP Endpoint",
        Description = "Handle an incoming HTTP request.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class HttpEndpoint : Activity
    {
        /// <summary>
        /// The path that triggers this activity. 
        /// </summary>
        [ActivityProperty(Hint = "The relative path that triggers this activity.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public PathString Path { get; set; }

        /// <summary>
        /// The HTTP methods that triggers this activity.
        /// </summary>
        [ActivityProperty(
            UIHint = ActivityPropertyUIHints.CheckList,
            Hint = "The HTTP methods that trigger this activity.",
            Options = new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS", "HEAD" },
            DefaultValue = new[] { "GET" },
            DefaultSyntax = SyntaxNames.Json,
            SupportedSyntaxes = new[] { SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public HashSet<string> Methods { get; set; } = new(new[] { "GET" });

        /// <summary>
        /// A value indicating whether the HTTP request content body should be read and stored as part of the HTTP request model.
        /// The stored format depends on the content-type header.
        /// </summary>
        [ActivityProperty(
            Hint = "A value indicating whether the HTTP request content body should be read and stored as part of the HTTP request model. The stored format depends on the content-type header.",
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public bool ReadContent { get; set; }

        /// <summary>
        /// The <see cref="Type"/> to parse the received request content into if <seealso cref="ReadContent"/> is set to true.
        /// If not set, the content will be parse into a default type, depending on the parser associated with the received content-type header.
        /// </summary>
        [ActivityProperty(Category = PropertyCategories.Advanced)]
        public Type? TargetType { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => context.WorkflowExecutionContext.IsFirstPass ? ExecuteInternal(context) : Suspend();
        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context) => ExecuteInternal(context);

        private IActivityExecutionResult ExecuteInternal(ActivityExecutionContext context)
        {
            var model = context.GetInput<HttpRequestModel>()!;
            return Done(model);
        }
    }
}