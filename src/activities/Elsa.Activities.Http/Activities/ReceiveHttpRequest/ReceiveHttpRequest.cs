using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Services;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    [ActivityDefinition(
        Category = "HTTP",
        DisplayName = "Receive HTTP Request",
        Description = "Receive an incoming HTTP request.",
        RuntimeDescription = "x => !!x.state.path ? `Handle <strong>${ x.state.method } ${ x.state.path }</strong>.` : x.definition.description",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class ReceiveHttpRequest : Activity
    {
        public static PathString GetPath(Variables state)
        {
            return state.GetState<PathString>(nameof(Path));
        }

        public static string GetMethod(Variables state)
        {
            return state.GetState<string>(nameof(Method));
        }

        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IEnumerable<IHttpRequestBodyParser> parsers;

        public ReceiveHttpRequest(
            IHttpContextAccessor httpContextAccessor,
            IEnumerable<IHttpRequestBodyParser> parsers)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.parsers = parsers;
        }

        /// <summary>
        /// The path that triggers this activity. 
        /// </summary>
        [ActivityProperty(Hint = "The relative path that triggers this activity.")]
        public PathString Path
        {
            get => GetState<PathString>();
            set => SetState(value);
        }

        /// <summary>
        /// The HTTP method that triggers this activity.
        /// </summary>
        [ActivityProperty(
            Type = ActivityPropertyTypes.Select,
            Hint = "The HTTP method that triggers this activity."
        )]
        [SelectOptions("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS", "HEAD")]
        public string Method
        {
            get => GetState<string>();
            set => SetState(value);
        }

        /// <summary>
        /// A value indicating whether the HTTP request content body should be read and stored as part of the HTTP request model.
        /// The stored format depends on the content-type header.
        /// </summary>
        [ActivityProperty(
            Hint =
                "A value indicating whether the HTTP request content body should be read and stored as part of the HTTP request model. The stored format depends on the content-type header."
        )]
        public bool ReadContent
        {
            get => GetState<bool>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            return context.WorkflowExecutionContext.IsFirstPass
                ? await ExecuteInternalAsync(context, cancellationToken)
                : Suspend();
        }

        protected override Task<IActivityExecutionResult> OnResumeAsync(ActivityExecutionContext context, CancellationToken cancellationToken) => ExecuteInternalAsync(context, cancellationToken);

        private async Task<IActivityExecutionResult> ExecuteInternalAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var request = httpContextAccessor.HttpContext.Request;
            var model = new HttpRequestModel
            {
                Path = new Uri(request.Path.ToString(), UriKind.Relative),
                QueryString = request.Query.ToDictionary(x => x.Key, x => new StringValuesModel(x.Value)),
                Headers = request.Headers.ToDictionary(x => x.Key, x => new StringValuesModel(x.Value)),
                Method = request.Method
            };

            if (ReadContent)
            {
                var parser = SelectContentParser(request.ContentType);
                model.Body = await parser.ParseAsync(request, cancellationToken);
            }

            return Done(Variable.From(model));
        }

        private IHttpRequestBodyParser SelectContentParser(string contentType)
        {
            var formatters = parsers.OrderByDescending(x => x.Priority).ToList();
            return formatters.FirstOrDefault(
                       x => x.SupportedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase)
                   ) ?? formatters.Last();
        }
    }
}