using Elsa.Common.Features;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Http.ContentWriters;
using Elsa.Http.Handlers;
using Elsa.Http.Implementations;
using Elsa.Http.Models;
using Elsa.Http.Options;
using Elsa.Http.Parsers;
using Elsa.Http.Services;
using Elsa.Mediator.Extensions;
using Elsa.Workflows.Management.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Http.Features;

[DependsOn(typeof(MemoryCacheFeature))]
public class HttpFeature : FeatureBase
{
    public HttpFeature(IModule module) : base(module)
    {
    }

    public Action<HttpActivityOptions>? ConfigureHttpOptions { get; set; } 
    public Func<IServiceProvider, IHttpEndpointAuthorizationHandler> HttpEndpointAuthorizationHandlerFactory { get; set; } = ActivatorUtilities.GetServiceOrCreateInstance<AllowAnonymousHttpEndpointAuthorizationHandler>;
    public Func<IServiceProvider, IHttpEndpointWorkflowFaultHandler> HttpEndpointWorkflowFaultHandlerFactory { get; set; } = ActivatorUtilities.GetServiceOrCreateInstance<DefaultHttpEndpointWorkflowFaultHandler>;

    /// <inheritdoc />
    public override void Configure()
    {
        Module.UseWorkflowManagement(management => management.AddVariableTypes(new[]
        {
            typeof(HttpRequestHeaders),
            typeof(HttpRequestModel),
            typeof(HttpResponseModel)
        }, "HTTP"));
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

        Services
            .AddHttpClient()
            .AddSingleton<IRouteMatcher, RouteMatcher>()
            .AddSingleton<IRouteTable, RouteTable>()
            .AddSingleton<IAbsoluteUrlProvider, DefaultAbsoluteUrlProvider>()
            .AddNotificationHandlersFrom<UpdateRouteTable>()
            .AddHttpContextAccessor()
            
            // Add Content Parsers
            .AddSingleton<IHttpResponseContentReader, JsonElementHttpResponseContentReader>()

            //Add Request Content Writers
            .AddSingleton<IHttpRequestContentWriter, StringHttpRequestContentWriter>()
            .AddSingleton<IHttpRequestContentWriter, FormUrlEncodedHttpRequestContentWriter>()
            ;
    }
}