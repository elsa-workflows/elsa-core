﻿using System;
using System.Collections.Generic;
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
        /// The body to send along with the request.
        /// </summary>
        public WorkflowExpression<string> Body { get; set; }
        
        /// <summary>
        /// The headers to send along with the request.
        /// </summary>
        public IDictionary<string, string> RequestHeaders { get; set; }

        /// <summary>
        /// A list of HTTP status codes this activity cam handle.
        /// </summary>
        public ICollection<int> SupportedStatusCodes { get; set; }
    }
}