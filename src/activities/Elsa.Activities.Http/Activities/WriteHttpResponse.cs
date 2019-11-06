using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Services;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Results;
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
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IEnumerable<IContentFormatter> contentFormatters;

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

        public WriteHttpResponse(
            IWorkflowExpressionEvaluator expressionEvaluator,
            IHttpContextAccessor httpContextAccessor,
            IEnumerable<IContentFormatter> contentFormatters)
        {
            this.expressionEvaluator = expressionEvaluator;
            this.httpContextAccessor = httpContextAccessor;
            this.contentFormatters = contentFormatters;
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
        public WorkflowExpression<string> Content
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
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
        public WorkflowExpression<string> ResponseHeaders
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }


        /// <summary>
        /// Variable name for model body - Read model body from named variable. 
        /// </summary>
        [ActivityProperty(Hint = "Variable name for content model - Read content model from named variable.")]
        public string InputVariable
        {
            get => GetState<string>(null, "InputVariable");
            set => SetState(value, "InputVariable");
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(
            WorkflowExecutionContext workflowContext,
            CancellationToken cancellationToken)
        {
            var response = httpContextAccessor.HttpContext.Response;

            if (response.HasStarted)
                return Fault("Response has already started");

            response.StatusCode = (int)StatusCode;
            response.ContentType = ContentType;

            var headersText = await expressionEvaluator.EvaluateAsync(
                ResponseHeaders,
                workflowContext,
                cancellationToken
            );

            if (headersText != null)
            {
                var headersQuery =
                    from line in Regex.Split(headersText, "\\n", RegexOptions.Multiline)
                    let pair = line.Split(':', '=')
                    select new KeyValuePair<string, string>(pair[0], pair[1]);

                foreach (var header in headersQuery)
                {
                    var headerValueExpression = new WorkflowExpression<string>(ResponseHeaders.Syntax, header.Value);
                    response.Headers[header.Key] = await expressionEvaluator.EvaluateAsync(
                        headerValueExpression,
                        workflowContext,
                        cancellationToken
                    );
                }
            }

            string bodyText = "";
            object inputVariableValue = workflowContext.GetVariable(InputVariable);

            if (inputVariableValue == null)
            {
                bodyText = await expressionEvaluator.EvaluateAsync(Content, workflowContext, cancellationToken);
            }
            else
            {
                bodyText = System.Text.Encoding.Default.GetString(
                    await SelectContentParser(ContentType).ParseAsync(inputVariableValue, ContentType)
                    );
            }

            if (!string.IsNullOrWhiteSpace(bodyText))
            {
                await response.WriteAsync(bodyText, cancellationToken);
            }

            return Done();
        }

        private IContentFormatter SelectContentParser(string contentType)
        {
            var formatters = contentFormatters.OrderByDescending(x => x.Priority).ToList();
            return formatters.FirstOrDefault(
                       x => x.SupportedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase)
                   ) ?? formatters.Last();
        }
    }


}