using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Services;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Extensions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace Elsa.Activities.Http.Activities
{
    [ActivityDefinition(
        Category = "HTTP",
        DisplayName = "Receive HTTP XML Request (type safe!)",
        Description = "Receive an incoming HTTP XML request. (type safe!)",
        RuntimeDescription = "x => !!x.state.path ? `Handle <strong>${ x.state.method } ${ x.state.path }</strong>.` : x.definition.description",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class ReceiveXmlHttpRequest : Activity
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

        public ReceiveXmlHttpRequest(
            IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// The path that triggers this activity. 
        /// </summary>
        [ActivityProperty(Hint = "The relative path that triggers this activity.")]
        public Uri Path
        {
            get => GetState<Uri>();
            set => SetState(value);
        }

        /// <summary>
        /// The HTTP method that triggers this activity.
        /// </summary>
        [ActivityProperty(
            Type = ActivityPropertyTypes.Select,
            Hint = "The HTTP method that triggers this activity."
        )]
        [SelectOptions("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS", "HEAD")]
        public string Method
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

        protected override ActivityExecutionResult OnExecute(WorkflowExecutionContext workflowContext)
        {
            return Halt(true);
        }

        protected override async Task<ActivityExecutionResult> OnResumeAsync(
            WorkflowExecutionContext workflowContext,
            CancellationToken cancellationToken)
        {
            var request = httpContextAccessor.HttpContext.Request;
            var model = new HttpRequestModel
            {
                Path = new Uri(request.Path.ToString(), UriKind.Relative),
                QueryString = request.Query.ToDictionary(x => x.Key, x => new StringValuesModel(x.Value)),
                Headers = request.Headers.ToDictionary(x => x.Key, x => new StringValuesModel(x.Value)),
                Method = request.Method
            };

            byte[] content = await request.ReadContentAsBytesAsync();

            string rootNodeTypeName = TypeName;
            if (string.IsNullOrEmpty(rootNodeTypeName))
                rootNodeTypeName = TypeSafeXMLExtensions.GetRootTypeName(ref content);

            if (rootNodeTypeName != "")
            {
                Type rootType = TypeSafeXMLExtensions.GetType(rootNodeTypeName, AssemlbyName);
                XmlSerializer xmlSerializer = new XmlSerializer(rootType);
                model.Body = xmlSerializer.Deserialize(new MemoryStream(content));
                workflowContext.CurrentScope.LastResult = Output.SetVariable("Content", model);
                Output.SetVariable("Result", model.Body);
                workflowContext.SetVariable("Result", model.Body);
            }

            return Done();
        }
    }
}