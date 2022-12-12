using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Elsa.Activities.Http.Contracts;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Options;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Metadata;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using HttpRequestHeaders = Elsa.Activities.Http.Models.HttpRequestHeaders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    [Action(
        Category = "HTTP",
        DisplayName = "Send HTTP Request",
        Description = "Send an HTTP request.",
        Outcomes = new[] { OutcomeNames.Done, "Unsupported Status Code" }
    )]
    public class SendHttpRequest : Activity, IActivityPropertyOptionsProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IEnumerable<IHttpResponseContentReader> _parsers;
        private readonly string? _defaultContentParserName;

        public SendHttpRequest(
            IHttpClientFactory httpClientFactory,
            IEnumerable<IHttpResponseContentReader> parsers,
            IOptions<HttpActivityOptions> options)
        {
            _httpClient = httpClientFactory.CreateClient(nameof(SendHttpRequest));
            _parsers = parsers;
            _defaultContentParserName = options.Value.DefaultContentParserName;
        }

        /// <summary>
        /// The URL to invoke. 
        /// </summary>
        [ActivityInput(Hint = "The URL to send the HTTP request to.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public Uri? Url { get; set; }

        /// <summary>
        /// The HTTP method to use.
        /// </summary>
        [ActivityInput(
            UIHint = ActivityInputUIHints.Dropdown,
            Hint = "The HTTP method to use when making the request.",
            Options = new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS", "HEAD" },
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? Method { get; set; }

        /// <summary>
        /// The body to send along with the request.
        /// </summary>
        [ActivityInput(Hint = "The HTTP content to send along with the request.", UIHint = ActivityInputUIHints.MultiLine, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public object? Content { get; set; }

        /// <summary>
        /// The Content Type header to send along with the request body.
        /// </summary>
        [ActivityInput(
            UIHint = ActivityInputUIHints.Dropdown,
            Hint = "The content type to send with the request.",
            Options = new[] { "", "text/plain", "text/html", "application/json", "application/xml", "application/x-www-form-urlencoded" },
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? ContentType { get; set; }

        /// <summary>
        /// The Authorization header value to send.
        /// </summary>
        /// <summary>
        /// The Authorization header value to send.
        /// </summary>
        [ActivityInput(
            Label = "Authorization",
            Hint = "The Authorization header value to send.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? Authorization { get; set; }


        /// <summary>
        /// The headers to send along with the request.
        /// </summary>
        [ActivityInput(
            Hint = "Additional headers to send along with the request.",
            UIHint = ActivityInputUIHints.MultiLine, DefaultSyntax = SyntaxNames.Json,
            SupportedSyntaxes = new[] { SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid },
            Category = PropertyCategories.Advanced
        )]
        public HttpRequestHeaders RequestHeaders { get; set; } = new();

        /// <summary>
        /// Read the content of the response.
        /// </summary>
        [ActivityInput(Hint = "Read the content of the response.", SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public bool ReadContent { get; set; }

        private string? _responseContentParserName;

        /// <summary>
        /// The parser to use to parse the response content. Plain Text, JSON, .NET Type, Expando Object, JToken, File
        /// </summary>
        [ActivityInput(
            Label = "Response Content Parser",
            Hint = "The parser to use to parse the response content.",
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid },
            UIHint = ActivityInputUIHints.Dropdown,
            OptionsProvider = typeof(SendHttpRequest)
        )]
        public string? ResponseContentParserName
        {
            get => _responseContentParserName;
            set
            {
                _responseContentParserName = value switch
                {
                    // Once upon a time there were two additional parser but were obsoleted
                    // because they did the same thing as existing ones.
                    // Automatically changing to appropriate name here so old workflows continue to work.
                    "Expando Object" => ".NET Type",
                    "JSON" => "Plain Text",
                    _ => value
                };
            }
        }

        /// <summary>
        /// The assembly-qualified .NET type name to deserialize the received content into.
        /// </summary>
        [ActivityInput(
            Label = "Response Content .NET Type",
            Hint = "The assembly-qualified .NET type name to deserialize the received content into.",
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid },
            OptionsProvider = typeof(SendHttpRequest)
        )]
        public Type? ResponseContentTargetType { get; set; }

        /// <summary>
        /// A list of HTTP status codes this activity can handle. 
        /// If the response's status code is in this list then an outcome with that status code is created. If it is not in this list then the outcome is <c>Unsupported Status Code</c>.
        /// </summary>
        [ActivityInput(
            Hint = "A list of possible HTTP status codes to handle.",
            UIHint = ActivityInputUIHints.MultiText,
            DefaultSyntax = SyntaxNames.Json,
            DefaultValue = new[] { 200 },
            SupportedSyntaxes = new[] { SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid },
            ConsiderValuesAsOutcomes = true,
            IsDesignerCritical = true
        )]
        public ICollection<int>? SupportedStatusCodes { get; set; } = new HashSet<int>(new[] { 200 });

        /// <summary>
        /// The status code and headers of HTTP response.
        /// </summary>
        [ActivityOutput]
        public HttpResponseModel? Response { get; set; }

        /// <summary>
        /// The content HTTP of the response formatted to <see cref="ResponseContentTargetType"/>
        /// </summary>
        [ActivityOutput]
        public object? ResponseContent { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var request = CreateRequest();
            var cancellationToken = context.CancellationToken;
            var response = (await _httpClient.SendAsync(request, cancellationToken))!;
            var hasContent = response.Content != null!;
            var contentType = response.Content?.Headers.ContentType?.MediaType;

            var allHeaders =
                response.Headers.ToDictionary(x => x.Key, x => x.Value.ToArray())
                    .Concat(response.Content?.Headers.ToDictionary(x => x.Key, x => x.Value.ToArray())!);

            var responseModel = new HttpResponseModel
            {
                StatusCode = response!.StatusCode,
                Headers = new Dictionary<string, string[]>(allHeaders)
            };

            if (hasContent && ReadContent)
            {
                // Only attempt to parse content if the status code represents success.
                if (response.IsSuccessStatusCode)
                {
                    var formatter = SelectContentParser(ResponseContentParserName, contentType);
                    ResponseContent = await formatter.ReadAsync(response, this, cancellationToken);
                }
                else
                {
                    ResponseContent = response.Content != null ? await response.Content.ReadAsStringAsync() : "";
                }
            }

            var statusCode = (int)response.StatusCode;
            var statusOutcome = statusCode.ToString();
            var supportedStatusCodes = SupportedStatusCodes;
            var isSupportedStatusCode = supportedStatusCodes == null || !supportedStatusCodes.Any() || SupportedStatusCodes?.Contains(statusCode) == true;
            var outcomes = new List<string> { OutcomeNames.Done, statusOutcome };

            if (!isSupportedStatusCode)
                outcomes.Add("Unsupported Status Code");

            Response = responseModel;
            return Outcomes(outcomes);
        }

        private IHttpResponseContentReader SelectContentParser(string? parserName, string? contentType)
        {
            if (string.IsNullOrWhiteSpace(parserName))
            {
                if (_defaultContentParserName != null)
                {
                    var defaultParser = _parsers.FirstOrDefault(x => x.Name == _defaultContentParserName);

                    if (defaultParser != null)
                        return defaultParser;
                }

                var simpleContentType = contentType?.Split(';').First() ?? "";
                var parser = _parsers.OrderByDescending(x => x.Priority).ToList();

                return parser.FirstOrDefault(x => x.GetSupportsContentType(simpleContentType)) ?? parser.Last();
            }
            else
            {
                var parser = _parsers.FirstOrDefault(x => x.Name == parserName);

                if (parser == null)
                    throw new InvalidOperationException("The specified parser does not exist");

                return parser;
            }
        }

        private HttpRequestMessage CreateRequest()
        {
            var method = Method ?? HttpMethods.Get;
            var methodSupportsBody = GetMethodSupportsBody(method);
            var url = Url;
            var request = new HttpRequestMessage(new HttpMethod(method), url);
            var authorizationHeaderValue = Authorization;
            var requestHeaders = new HeaderDictionary(RequestHeaders.ToDictionary(x => x.Key, x => new StringValues(x.Value.Split(','))));

            if (methodSupportsBody)
            {
                var bodyAsString = Content as string;
                var bodyAsBytes = Content as byte[];
                var contentType = ContentType;

                if (!string.IsNullOrWhiteSpace(bodyAsString))
                    request.Content = new StringContent(bodyAsString, Encoding.UTF8, contentType);
                else if (bodyAsBytes != null)
                {
                    request.Content = new ByteArrayContent(bodyAsBytes);
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
                }
            }

            if (!string.IsNullOrWhiteSpace(authorizationHeaderValue))
                request.Headers.Authorization = AuthenticationHeaderValue.Parse(authorizationHeaderValue);

            foreach (var header in requestHeaders)
                request.Headers.Add(header.Key, header.Value.AsEnumerable());

            return request;
        }

        object? IActivityPropertyOptionsProvider.GetOptions(PropertyInfo property)
        {
            if (property.Name != nameof(ResponseContentParserName))
                return null;

            var items = _parsers.Select(x => new SelectListItem(x.Name, x.Name)).ToList();

            items.Insert(0, new SelectListItem("Auto Select", ""));
            return items;
        }

        private static bool GetMethodSupportsBody(string method)
        {
            var methods = new[] { "POST", "PUT", "PATCH", "DELETE" };
            return methods.Contains(method, StringComparer.InvariantCultureIgnoreCase);
        }
    }
}