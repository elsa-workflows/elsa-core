using Elsa.Common.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Http.ContentWriters;
using Elsa.Http.Handlers;
using Elsa.Http.Implementations;
using Elsa.Http.Options;
using Elsa.Http.Parsers;
using Elsa.Http.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Http.Features;

[DependsOn(typeof(MemoryCacheFeature))]
public class HttpFeature : FeatureBase
{
    public HttpFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// A delegate to configure <see cref="HttpActivityOptions"/>.
    /// </summary>
    public Action<HttpActivityOptions>? ConfigureHttpOptions { get; set; }

    public Func<IServiceProvider, IHttpEndpointAuthorizationHandler> HttpEndpointAuthorizationHandler { get; set; } = ActivatorUtilities.GetServiceOrCreateInstance<AllowAnonymousHttpEndpointAuthorizationHandler>;
    public Func<IServiceProvider, IHttpEndpointWorkflowFaultHandler> HttpEndpointWorkflowFaultHandler { get; set; } = ActivatorUtilities.GetServiceOrCreateInstance<DefaultHttpEndpointWorkflowFaultHandler>;

    /// <summary>
    /// A delegate to configure the <see cref="HttpClient"/> used when by the <see cref="SendHttpRequest"/> activity.
    /// </summary>
    public Action<IServiceProvider, HttpClient> HttpClient { get; set; } = (_, _) => { };

    /// <summary>
    /// A delegate to configure the <see cref="HttpClientBuilder"/> for <see cref="HttpClient"/>.
    /// </summary>
    public Action<IHttpClientBuilder> HttpClientBuilder { get; set; } = _ => { };

    /// <inheritdoc />
    public override void Configure()
    {
        Module.UseWorkflowManagement(management =>
        {
            management.AddVariableTypes(new[]
            {
                typeof(RouteData),
                typeof(HttpRequest),
                typeof(HttpResponse)
            }, "HTTP");
            
            management.AddActivitiesFrom<HttpFeature>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        var configureOptions = ConfigureHttpOptions ?? (options =>
        {
            options.BasePath = "/workflows";
            options.BaseUrl = new Uri("http://localhost");
        });

        Services.Configure(configureOptions);

        var httpClientBuilder = Services.AddHttpClient<SendHttpRequest>(HttpClient);
        HttpClientBuilder(httpClientBuilder);

        Services
            .AddSingleton<IRouteMatcher, RouteMatcher>()
            .AddSingleton<IRouteTable, RouteTable>()
            .AddSingleton<IAbsoluteUrlProvider, DefaultAbsoluteUrlProvider>()
            .AddSingleton<IHttpBookmarkProcessor, HttpBookmarkProcessor>()
            .AddNotificationHandlersFrom<UpdateRouteTable>()
            .AddHttpContextAccessor()

            // Add content parsers.
            .AddSingleton<IHttpContentParser, StringHttpContentParser>()
            .AddSingleton<IHttpContentParser, JsonHttpContentParser>()
            .AddSingleton<IHttpContentParser, XmlHttpContentParser>()

            // Add HTTP content factories.
            .AddSingleton<IHttpContentFactory, TextContentFactory>()
            .AddSingleton<IHttpContentFactory, JsonContentFactory>()
            .AddSingleton<IHttpContentFactory, FormUrlEncodedHttpContentFactory>()
            ;
    }
}