using System.Collections.Generic;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Models;

namespace Elsa.Activities.Http.Activities
{
    [DisplayName("HTTP Request Action")]
    [Category("HTTP")]
    [Description("Performs an HTTP request.")]
    [DefaultEndpoint]
    public class HttpRequestAction : Activity
    {
        public HttpRequestAction()
        {
            SupportedStatusCodes = new HashSet<int>{ 200 };
        }
        
        /// <summary>
        /// The URL to invoke. 
        /// </summary>
        public WorkflowExpression<string> Url { get; set; }

        /// <summary>
        /// The HTTP method to use.
        /// </summary>
        public string Method { get; set; } = "GET";

        /// <summary>
        /// The body to send along with the request.
        /// </summary>
        public WorkflowExpression<string> Body { get; set; }

        /// <summary>
        /// The Content Type header to send along with the request body.
        /// </summary>
        public WorkflowExpression<string> ContentType { get; set; }

        /// <summary>
        /// The headers to send along with the request.
        /// </summary>
        public WorkflowExpression<string> RequestHeaders { get; set; }

        /// <summary>
        /// A list of HTTP status codes this activity can handle.
        /// </summary>
        public HashSet<int> SupportedStatusCodes { get; set; }
    }
}