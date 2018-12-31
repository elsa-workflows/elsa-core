using System;
using Elsa.Models;

namespace Elsa.Activities.Http.Activities
{
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
    }
}