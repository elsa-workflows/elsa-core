using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Activities
{
    [ActivityDefinition(
        Category = "HTTP",
        DisplayName = "Write HTTP Response",
        Description = "Write an HTTP response.",
        RuntimeDescription = "x => !!x.state.statusCode ? `Send an HTTP <strong>${ x.state.statusCode }</strong> - <strong>${ x.state.contentType }</strong> response` : x.definition.description",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class WriteHttpResponse : Activity
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public static IEnumerable<SelectItem> GetStatusCodes()
        {
            return new[]
            {
                new SelectItem
                {
                    Label = "2xx",
                    Options = new object[] { 200, 201, 202, 203, 204 }
                },
                new SelectItem
                {
                    Label = "3xx",
                    Options = new object[] { 301, 302, 304, 307, 308 }
                },
                new SelectItem
                {
                    Label = "4xx",
                    Options = new object[] { 400, 401, 402, 403, 404, 405, 409, 410, 412, 413, 415, 417, 418, 420, 428, 429 }
                }
            };
        }

        public WriteHttpResponse(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// The HTTP status code to return.
        /// </summary>
        [ActivityProperty(
            Type = ActivityPropertyTypes.Select,
            Hint = "The HTTP status code to write."
        )]
        [SelectOptions(nameof(GetStatusCodes), typeof(WriteHttpResponse))]
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
        [ActivityProperty(Hint = "The headers to send along with the response. One 'header: value' pair per line.")]
        public IWorkflowExpression<string> ResponseHeaders
        {
            get => GetState<IWorkflowExpression<string>>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var response = httpContextAccessor.HttpContext.Response;

            if (response.HasStarted)
                return Fault("Response has already started");

            response.StatusCode = (int)StatusCode;
            response.ContentType = ContentType;

            var headersText = await context.EvaluateAsync(ResponseHeaders, cancellationToken);

            if (headersText != null)
            {
                var headersQuery =
                    from line in Regex.Split(headersText, "\\n", RegexOptions.Multiline)
                    let pair = line.Split(':', '=')
                    select new KeyValuePair<string, string>(pair[0], pair[1]);

                foreach (var header in headersQuery) 
                    response.Headers[header.Key] = header.Value;
            }

            var bodyText = await context.EvaluateAsync(Content, cancellationToken);

            if (!string.IsNullOrWhiteSpace(bodyText)) 
                await response.WriteAsync(bodyText, cancellationToken);

            return Done();
        }
    }
}