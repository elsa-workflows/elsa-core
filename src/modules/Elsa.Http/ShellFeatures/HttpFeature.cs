using CShells.Features;
using Elsa.Expressions.Options;
using Elsa.Extensions;
using Elsa.Http.Bookmarks;
using Elsa.Http.ContentWriters;
using Elsa.Http.DownloadableContentHandlers;
using Elsa.Http.FileCaches;
using Elsa.Http.Handlers;
using Elsa.Http.Options;
using Elsa.Http.Parsers;
using Elsa.Http.PortResolvers;
using Elsa.Http.Selectors;
using Elsa.Http.Services;
using Elsa.Http.Tasks;
using Elsa.Http.TriggerPayloadValidators;
using Elsa.Http.UIHints;
using FluentStorage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Http.ShellFeatures;

/// <summary>
/// Installs services related to HTTP services and activities.
/// </summary>
[ShellFeature(
    DisplayName = "HTTP",
    Description = "Provides HTTP workflow activities, endpoints, and related services",
    DependsOn = ["Resilience"])]
public class HttpFeature : IShellFeature
{
    /// <summary>
    /// The base path for HTTP workflow endpoints. Defaults to "/workflows".
    /// </summary>
    public string BasePath { get; set; } = "/workflows";

    /// <summary>
    /// The base URL for generating absolute URLs. Defaults to "http://localhost".
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost";

    /// <summary>
    /// Time-to-live for cached files. Defaults to 7 days.
    /// </summary>
    public TimeSpan FileCacheTimeToLive { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Local cache directory for downloaded files. Defaults to "App_Data/FileCache".
    /// </summary>
    public string LocalCacheDirectory { get; set; } = Path.Combine(Environment.CurrentDirectory, "App_Data/FileCache");

    public void ConfigureServices(IServiceCollection services)
    {
        // Configure options
        services.Configure<HttpActivityOptions>(options =>
        {
            options.BasePath = BasePath;
            options.BaseUrl = new Uri(BaseUrl);
        });

        services.Configure<HttpFileCacheOptions>(options =>
        {
            options.TimeToLive = FileCacheTimeToLive;
            options.LocalCacheDirectory = LocalCacheDirectory;
        });

        // HTTP client
        services.AddHttpClient<SendHttpRequestBase>();

        services
            .AddScoped<IRouteMatcher, RouteMatcher>()
            .AddScoped<IRouteTable, RouteTable>()
            .AddScoped<IAbsoluteUrlProvider, DefaultAbsoluteUrlProvider>()
            .AddScoped<IRouteTableUpdater, DefaultRouteTableUpdater>()
            .AddScoped<IHttpWorkflowLookupService, HttpWorkflowLookupService>()
            .AddScoped<IContentTypeProvider>(_ => new FileExtensionContentTypeProvider())
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
            .AddScoped<IHttpEndpointBasePathProvider, DefaultHttpEndpointBasePathProvider>()

            // Port resolvers.
            .AddScoped<IActivityResolver, SendHttpRequestActivityResolver>()

            // HTTP endpoint handlers.
            .AddScoped<AuthenticationBasedHttpEndpointAuthorizationHandler>()
            .AddScoped<AllowAnonymousHttpEndpointAuthorizationHandler>()
            .AddScoped<DefaultHttpEndpointFaultHandler>()
            .AddScoped<DefaultHttpEndpointRoutesProvider>()
            .AddScoped<DefaultHttpEndpointBasePathProvider>()
            .AddScoped<IHttpEndpointFaultHandler, DefaultHttpEndpointFaultHandler>()
            .AddScoped<IHttpEndpointAuthorizationHandler, AuthenticationBasedHttpEndpointAuthorizationHandler>()
            .AddScoped<IHttpEndpointRoutesProvider, DefaultHttpEndpointRoutesProvider>()
            
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
            .AddScoped<IFileCacheStorageProvider>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<HttpFileCacheOptions>>().Value;
                var blobStorage = StorageFactory.Blobs.DirectoryFiles(options.LocalCacheDirectory);
                return new BlobFileCacheStorageProvider(blobStorage);
            })
            .AddScoped<ZipManager>()

            // Authorization services.
            .AddAuthorization();

        // HTTP clients.
        services.AddHttpClient<IFileDownloader, HttpClientFileDownloader>();

        // Add default selectors.
        services
            .AddScoped<IHttpCorrelationIdSelector, HeaderHttpCorrelationIdSelector>()
            .AddScoped<IHttpCorrelationIdSelector, QueryStringHttpCorrelationIdSelector>()
            .AddScoped<IHttpWorkflowInstanceIdSelector, HeaderHttpWorkflowInstanceIdSelector>()
            .AddScoped<IHttpWorkflowInstanceIdSelector, QueryStringHttpWorkflowInstanceIdSelector>();

        services.Configure<ExpressionOptions>(options =>
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
