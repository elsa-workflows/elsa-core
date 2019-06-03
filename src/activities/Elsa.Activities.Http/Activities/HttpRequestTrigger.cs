using System;
using Elsa.Attributes;
using Elsa.Models;

namespace Elsa.Activities.Http.Activities
{
    [DisplayName("HTTP Request Trigger")]
    [Category("HTTP")]
    [Description("Triggers when an incoming HTTP request is received.")]
    [IsTrigger]
    [DefaultEndpoint]
    public class HttpRequestTrigger : Activity
    {
        /// <summary>
        /// The path that triggers this activity. 
        /// </summary>
        public Uri Path { get; set; }

        /// <summary>
        /// The HTTP method that triggers this activity.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// A value indicating whether the HTTP request content body should be read and stored as part of the HTTP request model.
        /// The stored format depends on the content-type header.
        /// </summary>
        public bool ReadContent { get; set; }
    }
}