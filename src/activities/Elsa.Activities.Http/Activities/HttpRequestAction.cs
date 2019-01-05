using System;
using System.Collections.Generic;
using Elsa.Activities.Http.Models;
using Elsa.Expressions;
using Elsa.Models;

namespace Elsa.Activities.Http.Activities
{
    public class HttpRequestAction : Activity
    {
        /// <summary>
        /// The URL to invoke. 
        /// </summary>
        public Uri Url { get; set; }

        /// <summary>
        /// The HTTP method to use.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// The body to send along with the request
        /// </summary>
        public WorkflowExpression<string> Body { get; set; }
        
        /// <summary>
        /// The body to send along with the request
        /// </summary>
        public IDictionary<string, string> RequestHeaders { get; set; }
    }
}