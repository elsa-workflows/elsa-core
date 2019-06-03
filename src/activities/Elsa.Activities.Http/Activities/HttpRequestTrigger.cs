using System;
using System.ComponentModel.DataAnnotations;
using Elsa.Attributes;
using Elsa.Models;

namespace Elsa.Activities.Http.Activities
{
    [ActivityDisplayName("HTTP Request Trigger")]
    [ActivityCategory("HTTP")]
    [ActivityDescription("Triggers when an incoming HTTP request is received.")]
    [IsTrigger]
    [DefaultEndpoint]
    public class HttpRequestTrigger : Activity
    {
        /// <summary>
        /// The path that triggers this activity. 
        /// </summary>
        [Display(Description = "The relative path that triggers this activity.")]
        public Uri Path { get; set; }

        /// <summary>
        /// The HTTP method that triggers this activity.
        /// </summary>
        [Display(Description = "The HTTP method that triggers this activity.")]
        public string Method { get; set; }

        /// <summary>
        /// A value indicating whether the HTTP request content body should be read and stored as part of the HTTP request model.
        /// The stored format depends on the content-type header.
        /// </summary>
        [Display(Description = "A value indicating whether the HTTP request content body should be read and stored as part of the HTTP request model. The stored format depends on the content-type header.")]
        public bool ReadContent { get; set; }
    }
}