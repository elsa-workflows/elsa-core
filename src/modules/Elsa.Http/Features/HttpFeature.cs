using Elsa.Common.Multitenancy;
using Elsa.Expressions.Options;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Http.ContentWriters;
using Elsa.Http.Contracts;
using Elsa.Http.DownloadableContentHandlers;
using Elsa.Http.FileCaches;
using Elsa.Http.Handlers;
using Elsa.Http.HostedServices;
using Elsa.Http.Models;
using Elsa.Http.MultiTenancy;
using Elsa.Http.Options;
using Elsa.Http.Parsers;
using Elsa.Http.PortResolvers;
using Elsa.Http.Selectors;
using Elsa.Http.Services;
using Elsa.Http.UIHints;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Requests;
using Elsa.Workflows.Management.Responses;
using FluentStorage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Http.Features;

/// <summary>
/// Installs services related to HTTP services and activities.
/// </summary>
[DependsOn(typeof(HttpJavaScriptFeature))]
public class HttpFeature : FeatureBase
{
    /// <inheritdoc />
    public HttpFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// A delegate to configure <see cref="HttpActivityOptions"/>.
    /// </summary>
    public Action<HttpActivityOptions>? ConfigureHttpOptions { get; set; }

    /// <summary>
    /// A delegate to configure <see cref="HttpFileCacheOptions"/>.
    /// </summary>
    public Action<HttpFileCacheOptions>? ConfigureHttpFileCacheOptions { get; set; }

    /// <summary>
    /// A delegate that is invoked when authorizing an inbound HTTP request.
    /// </summary>
    public Func<IServiceProvider, IHttpEndpointAuthorizationHandler> HttpEndpointAuthorizationHandler { get; set; } = sp => sp.GetRequiredService<AuthenticationBasedHttpEndpointAuthorizationHandler>();

    /// <summary>
    /// A delegate that is invoked when an HTTP workflow faults.
    /// </summary>
    public Func<IServiceProvider, IHttpEndpointFaultHandler> HttpEndpointWorkflowFaultHandler { get; set; } = sp => sp.GetRequiredService<DefaultHttpEndpointFaultHandler>();

    /// <summary>
    /// A delegate to configure the <see cref="IContentTypeProvider"/>.
    /// </summary>
    public Func<IServiceProvider, IContentTypeProvider> ContentTypeProvider { get; set; } = _ => new FileExtensionContentTypeProvider();

    /// <summary>
    /// A delegate to configure the <see cref="IFileCacheStorageProvider"/>.
    /// </summary>
    public Func<IServiceProvider, IFileCacheStorageProvider> FileCache { get; set; } = sp =>
    {
        var options = sp.GetRequiredService<IOptions<HttpFileCacheOptions>>().Value;
        var blobStorage = StorageFactory.Blobs.DirectoryFiles(options.LocalCacheDirectory);
        return new BlobFileCacheStorageProvider(blobStorage);
    };

    /// <summary>
    /// A delegate to configure the <see cref="HttpClient"/> used when by the <see cref="FlowSendHttpRequest"/> activity.
    /// </summary>
    public Action<IServiceProvider, HttpClient> HttpClient { get; set; } = (_, _) => { };

    /// <summary>
    /// A delegate to configure the <see cref="HttpClientBuilder"/> for <see cref="HttpClient"/>.
    /// </summary>
    public Action<IHttpClientBuilder> HttpClientBuilder { get; set; } = _ => { };

    /// <summary>
    /// A list of <see cref="IHttpCorrelationIdSelector"/> types to register with the service collection.
    /// </summary>
    public ICollection<Type> HttpCorrelationIdSelectorTypes { get; } = new List<Type>
    {
        typeof(HeaderHttpCorrelationIdSelector),
        typeof(QueryStringHttpCorrelationIdSelector)
    };

    /// <summary>
    /// A list of <see cref="IHttpWorkflowInstanceIdSelector"/> types to register with the service collection.
    /// </summary>
    public ICollection<Type> HttpWorkflowInstanceIdSelectorTypes { get; } = new List<Type>
    {
        typeof(HeaderHttpWorkflowInstanceIdSelector),
        typeof(QueryStringHttpWorkflowInstanceIdSelector)
    };

    /// <inheritdoc />
    public override void Configure()
    {
        Module.UseWorkflowManagement(management =>
        {
            management.AddVariableTypes(new[]
            {
                typeof(RouteData),
                typeof(HttpRequest),
                typeof(HttpResponse),
                typeof(HttpResponseMessage),
                typeof(HttpHeaders),
                typeof(IFormFile),
                typeof(HttpFile),
                typeof(Downloadable)
            }, "HTTP");

            management.AddActivitiesFrom<HttpFeature>();
        });
    }

    /// <inheritdoc />
    public override void ConfigureHostedServices()
    {
        ConfigureHostedService<UpdateRouteTableHostedService>();
    }

    /// <inheritdoc />
    public override void Apply()
    {
        var configureOptions = ConfigureHttpOptions ?? (options =>
        {
            options.BasePath = "/workflows";
            options.BaseUrl = new Uri("http://localhost");
        });

        var configureFileCacheOptions = ConfigureHttpFileCacheOptions ?? (options => { options.TimeToLive = TimeSpan.FromDays(7); });

        Services.Configure(configureOptions);
        Services.Configure(configureFileCacheOptions);

        var httpClientBuilder = Services.AddHttpClient<SendHttpRequestBase>(HttpClient);
        HttpClientBuilder(httpClientBuilder);

        Services
            .AddScoped<IRouteMatcher, RouteMatcher>()
            .AddScoped<IRouteTable, RouteTable>()
            .AddScoped<IAbsoluteUrlProvider, DefaultAbsoluteUrlProvider>()
            .AddScoped<IRouteTableUpdater, DefaultRouteTableUpdater>()
            .AddScoped<IHttpWorkflowLookupService, HttpWorkflowLookupService>()
            .AddScoped(ContentTypeProvider)
            .AddHttpContextAccessor()

            // Handlers.
            .AddRequestHandler<ValidateWorkflowRequestHandler, ValidateWorkflowRequest, ValidateWorkflowResponse>()
            .AddNotificationHandler<UpdateRouteTable>()

            // Content parsers.
            .AddSingleton<IHttpContentParser, JsonHttpContentParser>()
            .AddSingleton<IHttpContentParser, XmlHttpContentParser>()
            .AddSingleton<IHttpContentParser, PlainTextHttpContentParser>()
            .AddSingleton<IHttpContentParser, TextHtmlHttpContentParser>()
            .AddSingleton<IHttpContentParser, FileHttpContentParser>()

            // HTTP content factories.
            .AddScoped<IHttpContentFactory, TextContentFactory>()
            .AddScoped<IHttpContentFactory, JsonContentFactory>()
            .AddScoped<IHttpContentFactory, XmlContentFactory>()
            .AddScoped<IHttpContentFactory, FormUrlEncodedHttpContentFactory>()

            // Activity property options providers.
            .AddScoped<IPropertyUIHandler, HttpContentTypeOptionsProvider>()

            // Port resolvers.
            .AddScoped<IActivityResolver, SendHttpRequestActivityResolver>()

            // HTTP endpoint handlers.
            .AddScoped<AuthenticationBasedHttpEndpointAuthorizationHandler>()
            .AddScoped<AllowAnonymousHttpEndpointAuthorizationHandler>()
            .AddScoped<DefaultHttpEndpointFaultHandler>()
            .AddScoped(HttpEndpointWorkflowFaultHandler)
            .AddScoped(HttpEndpointAuthorizationHandler)

            // Downloadable content handlers.
            .AddScoped<IDownloadableManager, DefaultDownloadableManager>()
            .AddScoped<IDownloadableContentHandler, MultiDownloadableContentHandler>()
            .AddScoped<IDownloadableContentHandler, BinaryDownloadableContentHandler>()
            .AddScoped<IDownloadableContentHandler, StreamDownloadableContentHandler>()
            .AddScoped<IDownloadableContentHandler, FormFileDownloadableContentHandler>()
            .AddScoped<IDownloadableContentHandler, DownloadableDownloadableContentHandler>()
            .AddScoped<IDownloadableContentHandler, UrlDownloadableContentHandler>()
            .AddScoped<IDownloadableContentHandler, StringDownloadableContentHandler>()
            .AddScoped<IDownloadableContentHandler, HttpFileDownloadableContentHandler>()

            // File caches.
            .AddScoped(FileCache)
            .AddScoped<ZipManager>()

            // AuthenticationBasedHttpEndpointAuthorizationHandler requires Authorization services.
            // We could consider creating a separate module for installing authorization services.
            .AddAuthorization();

        // HTTP clients.
        Services.AddHttpClient<IFileDownloader, HttpClientFileDownloader>();
        
        // Tenant resolvers.
        Services.AddScoped<ITenantResolutionStrategy, HttpContextTenantResolver>();
        Services.AddScoped<ITenantResolutionStrategy, RoutePrefixTenantResolver>();

        // Add selectors.
        foreach (var httpCorrelationIdSelectorType in HttpCorrelationIdSelectorTypes)
            Services.AddScoped(typeof(IHttpCorrelationIdSelector), httpCorrelationIdSelectorType);

        foreach (var httpWorkflowInstanceIdSelectorType in HttpWorkflowInstanceIdSelectorTypes)
            Services.AddScoped(typeof(IHttpWorkflowInstanceIdSelector), httpWorkflowInstanceIdSelectorType);

        Services.Configure<ExpressionOptions>(options =>
        {
            options.AddTypeAlias<HttpRequest>("HttpRequest");
            options.AddTypeAlias<HttpResponse>("HttpResponse");
            options.AddTypeAlias<HttpResponseMessage>("HttpResponseMessage");
            options.AddTypeAlias<HttpHeaders>("HttpHeaders");
            options.AddTypeAlias<RouteData>("RouteData");
            options.AddTypeAlias<IFormFile>("FormFile");
            options.AddTypeAlias<IFormFile[]>("FormFile[]");
            options.AddTypeAlias<HttpFile>("HttpFile");
            options.AddTypeAlias<HttpFile[]>("HttpFile[]");
            options.AddTypeAlias<Downloadable>("Downloadable");
            options.AddTypeAlias<Downloadable[]>("Downloadable[]");
        });
    }
}