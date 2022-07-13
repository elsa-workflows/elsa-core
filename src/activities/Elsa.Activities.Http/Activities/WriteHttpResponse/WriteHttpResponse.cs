using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Models;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Serialization;
using Elsa.Serialization.Converters;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.JsonNet;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    [Action(
        Category = "HTTP",
        DisplayName = "HTTP Response",
        Description = "Write an HTTP response.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class WriteHttpResponse : Activity
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IContentSerializer _contentSerializer;

        public WriteHttpResponse(IHttpContextAccessor httpContextAccessor, IStringLocalizer<WriteHttpResponse> localizer, IContentSerializer contentSerializer)
        {
            T = localizer;
            _httpContextAccessor = httpContextAccessor;
            _contentSerializer = contentSerializer;
        }

        private IStringLocalizer T { get; }

        /// <summary>
        /// The HTTP status code to return.
        /// </summary>
        [ActivityInput(
            UIHint = ActivityInputUIHints.Dropdown,
            Hint = "The HTTP status code to write.",
            Options = new[] { HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.Accepted, HttpStatusCode.NoContent, HttpStatusCode.Redirect, HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.Conflict },
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid },
            DefaultValue = HttpStatusCode.OK,
            Category = PropertyCategories.Advanced
        )]
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

        /// <summary>
        /// The content to send along with the response.
        /// </summary>
        [ActivityInput(Hint = "The HTTP content to write.", UIHint = ActivityInputUIHints.MultiLine, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public object? Content { get; set; }

        /// <summary>
        /// The Content-Type header to send along with the response.
        /// </summary>
        [ActivityInput(
            UIHint = ActivityInputUIHints.Dropdown,
            Hint = "The HTTP content type header to write.",
            Options = new[] { "text/plain", "text/html", "application/json", "application/xml" },
            DefaultValue = "text/plain",
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? ContentType { get; set; } = "text/plain";

        /// <summary>
        /// The character set to use when writing the response.
        /// </summary>
        [ActivityInput(
            Hint = "The character set to use when writing the response.",
            UIHint = ActivityInputUIHints.Dropdown,
            Options = new[] { "", "utf-8", "ASCII", "ANSI", "ISO-8859-1" },
            DefaultValue = "utf-8",
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid },
            Category = PropertyCategories.Advanced)]
        public string CharSet { get; set; } = "utf-8";

        /// <summary>
        /// The headers to send along with the response.
        /// </summary>
        [ActivityInput(
            Hint = "Additional headers to write.",
            UIHint = ActivityInputUIHints.MultiLine,
            DefaultSyntax = SyntaxNames.Json,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid, SyntaxNames.Json },
            Category = PropertyCategories.Advanced
        )]
        public HttpResponseHeaders? ResponseHeaders { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext();
            var response = httpContext.Response;

            if (response.HasStarted)
                return Fault(T["Response has already started"]!);

            response.StatusCode = (int) StatusCode;
            response.ContentType = string.IsNullOrWhiteSpace(CharSet) ? ContentType : $"{ContentType};charset={CharSet}";

            var headers = ResponseHeaders;

            if (headers != null)
            {
                foreach (var header in headers)
                    response.Headers[header.Key] = header.Value;
            }

            await WriteContentAsync(context.CancellationToken);

            return Done();
        }

        private async Task WriteContentAsync(CancellationToken cancellationToken)
        {
            var httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext();
            var response = httpContext.Response;
            
            var content = Content;

            if (content == null)
                return;
            
            if (content is string stringContent)
            {
                if (!string.IsNullOrWhiteSpace(stringContent))
                    await response.WriteAsync(stringContent, cancellationToken);

                return;
            }
            
            if (content is Stream stream) 
                content = await stream.ReadBytesToEndAsync(cancellationToken);

            if (content is byte[] buffer)
            {
                await response.Body.WriteAsync(buffer, cancellationToken);
                return;
            }

            var serializerSetting = CreateSerializerSettings();
            var json = JsonConvert.SerializeObject(content, serializerSetting);
            await response.WriteAsync(json, cancellationToken);
        }

        private JsonSerializerSettings CreateSerializerSettings()
        {
            var settings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize
            };
            
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy
                {
                    ProcessDictionaryKeys = false,
                    ProcessExtensionDataNames = true,
                    OverrideSpecifiedNames = false
                }
            };

            settings.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            settings.Converters.Add(new FlagEnumConverter(new DefaultNamingStrategy()));
            settings.Converters.Add(new TypeJsonConverter());
            return settings;
        }
    }
}