using System.Collections.Generic;
using System.Net;
using Elsa.Expressions;
using Elsa.Models;

namespace Elsa.Activities.Http.Activities
{
    public class HttpResponseAction : Activity
    {
        /// <summary>
        /// The HTTP status code to return.
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// The body to send along with the response
        /// </summary>
        public WorkflowExpression<string> Body { get; set; }
        
        /// <summary>
        /// The Content-Type header to send along with the response.
        /// </summary>
        public WorkflowExpression<string> ContentType { get; set; }
        
        /// <summary>
        /// The headers to send along with the response, one header: value pair per line.
        /// </summary>
        public WorkflowExpression<string> ResponseHeaders { get; set; }
    }
}