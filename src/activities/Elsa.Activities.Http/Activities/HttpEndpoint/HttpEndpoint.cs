using System;
using System.Collections.Generic;
using System.Reflection;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Providers.DefaultValues;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Metadata;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    /// <summary>
    /// Handle an incoming HTTP request.
    /// </summary>
    [Trigger(
        Category = "HTTP",
        DisplayName = "HTTP Endpoint",
        Description = "Handle an incoming HTTP request.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class HttpEndpoint : Activity, IActivityPropertyOptionsProvider
    {
        /// <summary>
        /// The path that triggers this activity. 
        /// </summary>
        [ActivityInput(Hint = "The relative path that triggers this activity.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string Path { get; set; }

        /// <summary>
        /// The HTTP methods that triggers this activity.
        /// </summary>
        [ActivityInput(
            UIHint = ActivityInputUIHints.CheckList,
            Hint = "The HTTP methods that trigger this activity.",
            Options = new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS", "HEAD" },
            DefaultValueProvider = typeof(HttpEndpointDefaultMethodsProvider),
            DefaultSyntax = SyntaxNames.Json,
            SupportedSyntaxes = new[] { SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid })]

        public HashSet<string> Methods { get; set; } = new() { "GET" };

        /// <summary>
        /// A value indicating whether the HTTP request content body should be read and stored as part of the HTTP request model.
        /// The stored format depends on the content-type header.
        /// </summary>
        [ActivityInput(
            Hint = "A value indicating whether the HTTP request content body should be read and stored as part of the HTTP request model. The stored format depends on the content-type header.",
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public bool ReadContent { get; set; }

        /// <summary>
        /// The <see cref="Type"/> to parse the received request content into if <seealso cref="ReadContent"/> is set to true.
        /// If not set, the content will be parse into a default type, depending on the parser associated with the received content-type header.
        /// </summary>
        [ActivityInput(Category = PropertyCategories.Advanced)]
        public Type? TargetType { get; set; }

        /// <summary>
        /// Schema for the HTTP Request model. This is used for autocomplete when referencing the HTTP request body in the Workflow Designer.
        /// You can use a tool like <see href="https://www.convertsimple.com/convert-json-to-json-schema/">Convert Simple's json to json schema converter</see> for schema generation.
        /// </summary>
        [ActivityInput(
            Category = PropertyCategories.Advanced,
            UIHint = ActivityInputUIHints.CodeEditor,
            OptionsProvider = typeof(HttpEndpoint))]
        public string? Schema { get; set; }

        /// <summary>
        /// Allow authenticated requests only
        /// </summary>
        [ActivityInput(
            Hint = "Check to only allow requests, which satisfy a specified policy",
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid },
            Category = "Security"
        )]
        public bool Authorize { get; set; }

        /// <summary>
        /// Provide a policy to challenge the user with. If the policy fails, the request is forbidden.
        /// </summary>
        [ActivityInput(
            Hint = "Provide a policy to evaluate. If the policy fails, the request is forbidden.",
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid },
            Category = "Security"
        )]
        public string? Policy { get; set; }
        
        [ActivityInput(
            Hint = "Check to only allow requests, which have a specified header with a specified value",
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid },
            Category = "Security"
        )]
        public bool AuthorizeWithCustomHeader { get; set; }
        
        [ActivityInput(
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid },
            Category = "Security"
        )]
        public string? CustomHeaderName { get; set; }
        
        [ActivityInput(
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid },
            Category = "Security"
        )]
        public string? CustomHeaderValue { get; set; }

        /// <summary>
        /// The received HTTP request.
        /// </summary>

        [ActivityOutput(Hint = "The received HTTP request.")]
        public HttpRequestModel? Output { get; set; }

        // protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => context.WorkflowExecutionContext.IsFirstPass ? ExecuteInternal(context) : Suspend();
        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            if(Path.Contains("//"))
                throw new Exception("Path cannot contain double slashes (//)");
            return context.WorkflowExecutionContext.IsFirstPass ? ExecuteInternal(context) : Suspend();
        }

        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context) => ExecuteInternal(context);

        private IActivityExecutionResult ExecuteInternal(ActivityExecutionContext context)
        {
            Output = context.GetInput<HttpRequestModel>()!;
            context.JournalData.Add("Inbound Request", Output);
            return Done();
        }

        object IActivityPropertyOptionsProvider.GetOptions(PropertyInfo property)
        {
            if (property.Name != nameof(Schema))
                return default!;

            return new
            {
                EditorHeight = "Large",
                Context = nameof(HttpEndpoint),
                Syntax = SyntaxNames.Json
            };
        }
    }
}