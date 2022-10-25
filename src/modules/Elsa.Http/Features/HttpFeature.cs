using System;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Http.ContentWriters;
using Elsa.Http.Handlers;
using Elsa.Http.Implementations;
using Elsa.Http.Options;
using Elsa.Http.Parsers;
using Elsa.Http.Services;
using Elsa.Mediator.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Http.Features;

public class HttpFeature : FeatureBase
{
    public HttpFeature(IModule module) : base(module)
    {
    }

    public string BasePath { get; set; } = "/workflows";
    public Func<IServiceProvider, IHttpEndpointAuthorizationHandler> HttpEndpointAuthorizationHandlerFactory { get; set; } = ActivatorUtilities.GetServiceOrCreateInstance<AllowAnonymousHttpEndpointAuthorizationHandler>;
    public Func<IServiceProvider, IHttpEndpointWorkflowFaultHandler> HttpEndpointWorkflowFaultHandlerFactory { get; set; } = ActivatorUtilities.GetServiceOrCreateInstance<DefaultHttpEndpointWorkflowFaultHandler>;

    public HttpFeature WithBasePath(string value)
    {
        BasePath = value;
        return this;
    }

    public HttpFeature WithAuthorizationHandlerFactory(Func<IServiceProvider, IHttpEndpointAuthorizationHandler> value)
    {
        HttpEndpointAuthorizationHandlerFactory = value;
        return this;
    }

    public HttpFeature WithWorkflowFaultHandlerFactory(Func<IServiceProvider, IHttpEndpointWorkflowFaultHandler> value)
    {
        HttpEndpointWorkflowFaultHandlerFactory = value;
        return this;
    }

    public override void Apply()
    {
        Services.Configure<HttpActivityOptions>(options => options.BasePath = BasePath);

        Services
            .AddHttpClient()
            .AddSingleton<IRouteMatcher, RouteMatcher>()
            .AddSingleton<IRouteTable, RouteTable>()
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