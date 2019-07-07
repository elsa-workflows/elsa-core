using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Services;
using Elsa.Core.Services;
using Elsa.Extensions;
using Elsa.Results;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace Elsa.Activities.Http.Activities
{
    public class HttpRequestTrigger : Activity
    {
        public static Uri GetPath(JObject state)
        {
            return state.GetState<Uri>(nameof(Path));
        }
        
        public static string GetMethod(JObject state)
        {
            return state.GetState<string>(nameof(Method));
        }
        
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IEnumerable<IContentFormatter> contentFormatters;

        public HttpRequestTrigger(
            IHttpContextAccessor httpContextAccessor,
            IEnumerable<IContentFormatter> contentFormatters)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.contentFormatters = contentFormatters;
        }

        /// <summary>
        /// The path that triggers this activity. 
        /// </summary>
        [Display(Description = "The relative path that triggers this activity.")]
        [Required]
        [UIHint("RelativePath")]
        public Uri Path
        {
            get => GetState<Uri>();
            set => SetState(value);
        }

        /// <summary>
        /// The HTTP method that triggers this activity.
        /// </summary>
        [Display(Description = "The HTTP method that triggers this activity.")]
        [Required]
        [UIHint("Dropdown")]
        public string Method {
            get => GetState<string>();
            set => SetState(value);
        }

        /// <summary>
        /// A value indicating whether the HTTP request content body should be read and stored as part of the HTTP request model.
        /// The stored format depends on the content-type header.
        /// </summary>
        [Display(Description = "A value indicating whether the HTTP request content body should be read and stored as part of the HTTP request model. The stored format depends on the content-type header.")]
        public bool ReadContent
        {
            get => GetState<bool>();
            set => SetState(value);
        }

        protected override ActivityExecutionResult OnExecute(WorkflowExecutionContext workflowContext)
        {
            return Halt(true);
        }

        protected override async Task<ActivityExecutionResult> OnResumeAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var request = httpContextAccessor.HttpContext.Request;
            var model = new HttpRequestModel
            {
                Path = new Uri(request.Path.ToString(), UriKind.Relative),
                QueryString = request.Query.ToDictionary(x => x.Key, x => x.Value),
                Headers = request.Headers.ToDictionary(x => x.Key, x => x.Value),
                Method = request.Method
            };

            if (ReadContent)
            {
                if (request.HasFormContentType)
                {
                    model.Form = (await request.ReadFormAsync(cancellationToken)).ToDictionary(x => x.Key, x => x.Value);
                }

                var parser = SelectContentParser(request.ContentType);
                var content = await request.ReadBodyAsync();
                model.Content = content;
                model.ParsedContent = await parser.ParseAsync(content, request.ContentType);
            }

            workflowContext.CurrentScope.LastResult = model;

            return Done();
        }

        private IContentFormatter SelectContentParser(string contentType)
        {
            var formatters = contentFormatters.OrderByDescending(x => x.Priority).ToList();
            return formatters.FirstOrDefault(x => x.SupportedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase)) ?? formatters.Last();
        }
    }
}