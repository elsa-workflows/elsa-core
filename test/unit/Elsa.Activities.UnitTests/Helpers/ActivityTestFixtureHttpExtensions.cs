using System.Net;
using System.Text;
using Elsa.Http;
using Elsa.Http.ContentWriters;
using Elsa.Http.Parsers;
using Elsa.Resilience;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Activities.UnitTests.Helpers;

/// <summary>
/// Extension methods for configuring HTTP-related services in ActivityTestFixture.
/// </summary>
public static class ActivityTestFixtureHttpExtensions
{
    /// <summary>
    /// Configures services commonly needed for HTTP activity testing.
    /// This is a convenience method that combines HTTP services, mock HTTP client factory, and mock resilient invoker.
    /// </summary>
    /// <param name="fixture">The test fixture to configure</param>
    /// <param name="responseHandler">Handler for HTTP requests, or null to use a default OK response</param>
    /// <returns>The fixture instance for method chaining</returns>
    public static ActivityTestFixture WithHttpServices(
        this ActivityTestFixture fixture,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? responseHandler = null)
    {
        fixture.ConfigureServices(services =>
        {
            var defaultHandler = responseHandler ?? ((_, _) => Task.FromResult(CreateHttpResponse(HttpStatusCode.OK)));
            var mockHttpClientFactory = CreateMockHttpClientFactory(defaultHandler);

            services.AddSingleton(mockHttpClientFactory);
            services.AddSingleton(CreateMockResilientActivityInvoker());
            AddHttpServices(services);
            services.AddLogging();
        });

        return fixture;
    }

    /// <summary>
    /// Creates a simple HTTP response message with specified status code and optional JSON content.
    /// </summary>
    /// <param name="statusCode">The HTTP status code for the response</param>
    /// <param name="jsonContent">Optional JSON content for the response body</param>
    /// <param name="additionalHeaders">Optional additional headers to add to the response</param>
    /// <returns>An HttpResponseMessage configured with the specified parameters</returns>
    public static HttpResponseMessage CreateHttpResponse(
        HttpStatusCode statusCode,
        string? jsonContent = null,
        Dictionary<string, string>? additionalHeaders = null)
    {
        var response = new HttpResponseMessage(statusCode);

        if (jsonContent != null)
        {
            response.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        }

        if (additionalHeaders != null)
        {
            foreach (var header in additionalHeaders)
            {
                response.Headers.Add(header.Key, header.Value);
            }
        }

        return response;
    }

    /// <summary>
    /// Adds all HTTP-related services to the service collection.
    /// This includes content factories, parsers, and HTTP client services.
    /// </summary>
    private static void AddHttpServices(IServiceCollection services)
    {
        // Add all required HTTP services
        services.AddSingleton<IHttpContentFactory, JsonContentFactory>();
        services.AddSingleton<IHttpContentFactory, TextContentFactory>();
        services.AddSingleton<IHttpContentFactory, XmlContentFactory>();
        services.AddSingleton<IHttpContentFactory, FormUrlEncodedHttpContentFactory>();

        // Add HTTP content parsers
        AddHttpContentParsers(services);

        // Add other required services
        services.AddHttpClient();
    }

    /// <summary>
    /// Adds HTTP content parsers to the service collection.
    /// These parsers are responsible for parsing different content types in HTTP responses.
    /// </summary>
    private static void AddHttpContentParsers(IServiceCollection services)
    {
        services.AddSingleton<IHttpContentParser, JsonHttpContentParser>();
        services.AddSingleton<IHttpContentParser, PlainTextHttpContentParser>();
        services.AddSingleton<IHttpContentParser, XmlHttpContentParser>();
        services.AddSingleton<IHttpContentParser, TextHtmlHttpContentParser>();
        services.AddSingleton<IHttpContentParser, FileHttpContentParser>();
    }

    /// <summary>
    /// Creates a mock IResilientActivityInvoker that directly executes the provided action.
    /// Useful for testing activities that depend on resilient execution without the complexity of retry policies.
    /// </summary>
    private static IResilientActivityInvoker CreateMockResilientActivityInvoker()
    {
        var mock = Substitute.For<IResilientActivityInvoker>();

        // Configure the mock to simply execute the provided action directly
        mock.InvokeAsync(
                Arg.Any<IResilientActivity>(),
                Arg.Any<ActivityExecutionContext>(),
                Arg.Any<Func<Task<HttpResponseMessage>>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var action = callInfo.ArgAt<Func<Task<HttpResponseMessage>>>(2);
                return action.Invoke();
            });

        return mock;
    }

    /// <summary>
    /// Creates a mock HTTP client factory with a test handler for controlled HTTP responses.
    /// </summary>
    private static IHttpClientFactory CreateMockHttpClientFactory(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseHandler)
    {
        var mockHttpClientFactory = Substitute.For<IHttpClientFactory>();
        var testHandler = new TestHttpMessageHandler(responseHandler);
        var httpClient = new HttpClient(testHandler);
        mockHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);
        return mockHttpClientFactory;
    }
}
