using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Models;
using Elsa.Attributes;
using Elsa.Design;
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
        DisplayName = "Write HTTP Response",
        Description = "Write an HTTP response.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class WriteHttpResponse : Activity
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public WriteHttpResponse(IHttpContextAccessor httpContextAccessor, IStringLocalizer<WriteHttpResponse> localizer)
        {
            T = localizer;
            this.httpContextAccessor = httpContextAccessor;
        }
        
        public IStringLocalizer T { get; }

        /// <summary>
        /// The HTTP status code to return.
        /// </summary>
        [ActivityProperty(
            Type = ActivityPropertyTypes.Select,
            Hint = "The HTTP status code to write."
        )]
        public HttpStatusCode StatusCode
        {
            get => GetState(() => HttpStatusCode.OK);
            set => SetState(value);
        }

        /// <summary>
        /// The content to send along with the response
        /// </summary>
        [ActivityProperty(Hint = "The HTTP content to write.")]
        [ExpressionOptions(Multiline = true)]
        public IWorkflowExpression<string> Content
        {
            get => GetState<IWorkflowExpression<string>>();
            set => SetState(value);
        }

        /// <summary>
        /// The Content-Type header to send along with the response.
        /// </summary>
        [ActivityProperty(
            Type = ActivityPropertyTypes.Select,
            Hint = "The HTTP content type header to write."
        )]
        [SelectOptions("text/plain", "text/html", "application/json", "application/xml")]
        public string ContentType
        {
            get => GetState<string>();
            set => SetState(value);
        }

        /// <summary>
        /// The headers to send along with the response. One 'header: value' pair per line.
        /// </summary>
        [ActivityProperty(Hint = "The headers to send along with the response.")]
        public IWorkflowExpression<HttpResponseHeaders>? ResponseHeaders
        {
            get => GetState<IWorkflowExpression<HttpResponseHeaders>>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var response = httpContextAccessor.HttpContext.Response;

            if (response.HasStarted)
                return Fault(T["Response has already started"]);

            response.StatusCode = (int)StatusCode;
            response.ContentType = ContentType;

            var headers = await context.EvaluateAsync(ResponseHeaders, cancellationToken);

            if (headers != null)
            {
                foreach (var header in headers) 
                    response.Headers[header.Key] = header.Value;
            }

            var bodyText = await context.EvaluateAsync(Content, cancellationToken);

            if (!string.IsNullOrWhiteSpace(bodyText)) 
                await response.WriteAsync(bodyText, cancellationToken);

            return Done();
        }
    }
}