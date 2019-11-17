using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Elsa.Activities.Http.Extensions;
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
        DisplayName = "Write HTTP XML Response (type-safe!)",
        Description = "Write an HTTP XML response. (type-safe!)",
        RuntimeDescription = "x => !!x.state.statusCode ? `Send an HTTP <strong>${ x.state.statusCode }</strong> - <strong>${ x.state.contentType }</strong> response` : x.definition.description",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class WriteXmlHttpResponse : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;
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

        public WriteXmlHttpResponse(
            IHttpContextAccessor httpContextAccessor,
            IWorkflowExpressionEvaluator expressionEvaluator)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.expressionEvaluator = expressionEvaluator;
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
        /// The Content-Type header to send along with the response.
        /// </summary>
        [ActivityProperty(
            Type = ActivityPropertyTypes.Select,
            Hint = "The HTTP content type header to write."
        )]
        [SelectOptions("application/xml", "text/plain", "text/html", "text/xml")]
        public string ContentType
        {
            get => GetState<string>();
            set => SetState(value);
        }

        /// <summary>
        /// The type of the received document.
        /// </summary>
        [ActivityProperty(
            Hint = "The type name of the received document. (Only Typename or with namespace)"
        )]
        public string TypeName
        {
            get => GetState<string>();
            set => SetState(value);
        }

        /// <summary>
        /// The library to load (e.g. dll file without path).
        /// </summary>
        [ActivityProperty(
            Hint = "The assembly to load (e.g. poco-dll file without path)."
        )]
        public string AssemlbyName
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


            string bodyText;
            Type rootType = TypeSafeXMLExtensions.GetType(TypeName, AssemlbyName);
            var inputObject = workflowContext.GetVariable("Result") != null ? workflowContext.GetVariable("Result") : workflowContext.CurrentScope.LastResult;

            
            XmlSerializer xmlSerializer = new XmlSerializer(rootType);
            using (MemoryStream stream = new MemoryStream())
            {
                xmlSerializer.Serialize(stream, inputObject);
                bodyText = System.Text.Encoding.Default.GetString(stream.ToArray());
            }


            if (!string.IsNullOrWhiteSpace(bodyText))
            {
                await response.WriteAsync(bodyText, cancellationToken);
            }

            return Done();
        }
    }
}











