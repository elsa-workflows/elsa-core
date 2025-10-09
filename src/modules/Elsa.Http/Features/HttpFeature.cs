using Elsa.Expressions.Options;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Http.Bookmarks;
using Elsa.Http.ContentWriters;
using Elsa.Http.DownloadableContentHandlers;
using Elsa.Http.FileCaches;
using Elsa.Http.Handlers;
using Elsa.Http.Options;
using Elsa.Http.Parsers;
using Elsa.Http.PortResolvers;
using Elsa.Http.Resilience;
using Elsa.Http.Selectors;
using Elsa.Http.Services;
using Elsa.Http.Tasks;
using Elsa.Http.TriggerPayloadValidators;
using Elsa.Http.UIHints;
using Elsa.Resilience.Extensions;
using Elsa.Resilience.Features;
using Elsa.Workflows;
using FluentStorage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Elsa.Http.Features;

/// <summary>
/// Installs services related to HTTP services and activities.
/// </summary>
[DependsOn(typeof(HttpJavaScriptFeature))]
[DependsOn(typeof(ResilienceFeature))]
public class HttpFeature(IModule module) : FeatureBase(module)
{
    private Func<IServiceProvider, IHttpEndpointRoutesProvider> _httpEndpointRouteProvider = sp => sp.GetRequiredService<DefaultHttpEndpointRoutesProvider>();
    private Func<IServiceProvider, IHttpEndpointBasePathProvider> _httpEndpointBasePathProvider = sp => sp.GetRequiredService<DefaultHttpEndpointBasePathProvider>();

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
    /// A delegate to configure the <see cref="HttpClient"/> used when by the <see cref="FlowSendHttpRequest"/> and <see cref="SendHttpRequest"/> activities.
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

    public HttpFeature WithHttpEndpointRoutesProvider<T>() where T : IHttpEndpointRoutesProvider
    {
        return WithHttpEndpointRoutesProvider(sp => sp.GetRequiredService<T>());
    }

    public HttpFeature WithHttpEndpointRoutesProvider(Func<IServiceProvider, IHttpEndpointRoutesProvider> httpEndpointRouteProvider)
    {
        _httpEndpointRouteProvider = httpEndpointRouteProvider;
        return this;
    }
    
    public HttpFeature WithHttpEndpointBasePathProvider<T>() where T : class, IHttpEndpointBasePathProvider
    {
        Services.TryAddScoped<T>();
        return WithHttpEndpointBasePathProvider(sp => sp.GetRequiredService<T>());
    }
    
    public HttpFeature WithHttpEndpointBasePathProvider(Func<IServiceProvider, IHttpEndpointBasePathProvider> httpEndpointBasePathProvider)
    {
        _httpEndpointBasePathProvider = httpEndpointBasePathProvider;
        return this;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.UseWorkflowManagement(management =>
        {
            management.AddVariableTypes([
                typeof(HttpRouteData),
                typeof(HttpRequest),
                typeof(HttpResponse),
                typeof(HttpResponseMessage),
                typeof(HttpHeaders),
                typeof(IFormFile),
                typeof(HttpFile),
                typeof(Downloadable)
            ], "HTTP");

            management.AddActivitiesFrom<HttpFeature>();
        });

        Module.UseResilience(resilience => resilience.AddResilienceStrategyType<HttpResilienceStrategy>());
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
            .AddScoped<IPropertyUIHandler, HttpEndpointPathUIHandler>()
            .AddScoped(_httpEndpointBasePathProvider)

            // Port resolvers.
            .AddScoped<IActivityResolver, SendHttpRequestActivityResolver>()

            // HTTP endpoint handlers.
            .AddScoped<AuthenticationBasedHttpEndpointAuthorizationHandler>()
            .AddScoped<AllowAnonymousHttpEndpointAuthorizationHandler>()
            .AddScoped<DefaultHttpEndpointFaultHandler>()
            .AddScoped<DefaultHttpEndpointRoutesProvider>()
            .AddScoped<DefaultHttpEndpointBasePathProvider>()
            .AddScoped(HttpEndpointWorkflowFaultHandler)
            .AddScoped(HttpEndpointAuthorizationHandler)
            .AddScoped(_httpEndpointRouteProvider)
            
            // Startup tasks.
            .AddStartupTask<UpdateRouteTableStartupTask>()

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

            //Trigger payload validators.
            .AddTriggerPayloadValidator<HttpEndpointTriggerPayloadValidator, HttpEndpointBookmarkPayload>()

            // File caches.
            .AddScoped(FileCache)
            .AddScoped<ZipManager>()

            // AuthenticationBasedHttpEndpointAuthorizationHandler requires Authorization services.
            // We could consider creating a separate module for installing authorization services.
            .AddAuthorization();

        // HTTP clients.
        Services.AddHttpClient<IFileDownloader, HttpClientFileDownloader>();

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
            options.AddTypeAlias<HttpRouteData>("RouteData");
            options.AddTypeAlias<IFormFile>("FormFile");
            options.AddTypeAlias<IFormFile[]>("FormFile[]");
            options.AddTypeAlias<HttpFile>("HttpFile");
            options.AddTypeAlias<HttpFile[]>("HttpFile[]");
            options.AddTypeAlias<Downloadable>("Downloadable");
            options.AddTypeAlias<Downloadable[]>("Downloadable[]");
        });
    }
}