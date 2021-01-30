using System.Net;
using System.Threading.Tasks;
using Elsa.Activities.Http.Models;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    [Action(
        Category = "HTTP",
        DisplayName = "Write HTTP Response",
        Description = "Write an HTTP response.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class WriteHttpResponse : Activity
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public WriteHttpResponse(IHttpContextAccessor httpContextAccessor, IStringLocalizer<WriteHttpResponse> localizer)
        {
            T = localizer;
            _httpContextAccessor = httpContextAccessor;
        }

        private IStringLocalizer T { get; }

        /// <summary>
        /// The HTTP status code to return.
        /// </summary>
        [ActivityProperty(
            Type = ActivityPropertyTypes.Select,
            Hint = "The HTTP status code to write."
        )]
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// The content to send along with the response
        /// </summary>
        [ActivityProperty(Hint = "The HTTP content to write.")]
        [WorkflowExpressionOptions(Multiline = true)]
        public string? Content { get; set; }

        /// <summary>
        /// The Content-Type header to send along with the response.
        /// </summary>
        [ActivityProperty(
            Type = ActivityPropertyTypes.Select,
            Hint = "The HTTP content type header to write."
        )]
        [SelectOptions("text/plain", "text/html", "application/json", "application/xml")]
        public string? ContentType { get; set; }

        /// <summary>
        /// The headers to send along with the response.
        /// </summary>
        [ActivityProperty(Hint = "The headers to send along with the response.")]
        public HttpResponseHeaders? ResponseHeaders { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var response = _httpContextAccessor.HttpContext.Response;

            if (response.HasStarted)
                return Fault(T["Response has already started"]!);

            response.StatusCode = (int)StatusCode;
            response.ContentType = ContentType;

            var headers = ResponseHeaders;

            if (headers != null)
            {
                foreach (var header in headers)
                    response.Headers[header.Key] = header.Value;
            }

            var bodyText = Content;

            if (!string.IsNullOrWhiteSpace(bodyText))
                await response.WriteAsync(bodyText, context.CancellationToken);

            return Done();
        }
    }
}