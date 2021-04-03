using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Services;
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
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEnumerable<IHttpRequestBodyParser> _parsers;

        public HttpEndpoint(
            IHttpContextAccessor httpContextAccessor,
            IEnumerable<IHttpRequestBodyParser> parsers)
        {
            _httpContextAccessor = httpContextAccessor;
            _parsers = parsers;
        }

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

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context) => context.WorkflowExecutionContext.IsFirstPass ? await ExecuteInternalAsync(context.CancellationToken) : Suspend();

        protected override ValueTask<IActivityExecutionResult> OnResumeAsync(ActivityExecutionContext context) => ExecuteInternalAsync(context.CancellationToken);

        private async ValueTask<IActivityExecutionResult> ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            var request = _httpContextAccessor.HttpContext.Request;

            var model = new HttpRequestModel
            {
                Path = new Uri(request.Path.ToString(), UriKind.Relative),
                QueryString = request.Query.ToDictionary(x => x.Key, x => x.Value.ToString()),
                Headers = request.Headers.ToDictionary(x => x.Key, x => x.Value.ToString()),
                Method = request.Method
            };

            if (ReadContent)
            {
                var parser = SelectContentParser(request.ContentType);
                model.Body = await parser.ParseAsync(request, TargetType, cancellationToken);
            }

            return Done(model);
        }

        private IHttpRequestBodyParser SelectContentParser(string contentType)
        {
            var simpleContentType = contentType?.Split(';').First();
            var formatters = _parsers.OrderByDescending(x => x.Priority).ToList();
            return formatters.FirstOrDefault(x => x.SupportedContentTypes.Contains(simpleContentType, StringComparer.OrdinalIgnoreCase)) ?? formatters.Last();
        }
    }
}