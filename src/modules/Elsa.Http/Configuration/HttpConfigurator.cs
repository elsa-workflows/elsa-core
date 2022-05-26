using System;
using Elsa.Http.Handlers;
using Elsa.Http.Implementations;
using Elsa.Http.Options;
using Elsa.Http.Services;
using Elsa.Mediator.Extensions;
using Elsa.ServiceConfiguration.Abstractions;
using Elsa.ServiceConfiguration.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Http.Configuration;

public class HttpConfigurator : ConfiguratorBase
{
    public HttpConfigurator(IServiceConfiguration serviceConfiguration) : base(serviceConfiguration)
    {
    }

    public string BasePath { get; set; } = "/workflows";
    public Func<IServiceProvider, IHttpEndpointAuthorizationHandler> HttpEndpointAuthorizationHandlerFactory { get; set; } = ActivatorUtilities.GetServiceOrCreateInstance<AllowAnonymousHttpEndpointAuthorizationHandler>;
    public Func<IServiceProvider, IHttpEndpointWorkflowFaultHandler> HttpEndpointWorkflowFaultHandlerFactory { get; set; } = ActivatorUtilities.GetServiceOrCreateInstance<DefaultHttpEndpointWorkflowFaultHandler>;

    public HttpConfigurator WithBasePath(string value)
    {
        BasePath = value;
        return this;
    }

    public HttpConfigurator WithAuthorizationHandlerFactory(Func<IServiceProvider, IHttpEndpointAuthorizationHandler> value)
    {
        HttpEndpointAuthorizationHandlerFactory = value;
        return this;
    }

    public HttpConfigurator WithWorkflowFaultHandlerFactory(Func<IServiceProvider, IHttpEndpointWorkflowFaultHandler> value)
    {
        HttpEndpointWorkflowFaultHandlerFactory = value;
        return this;
    }

    public override void Apply()
    {
        Services.Configure<HttpActivityOptions>(options => options.BasePath = BasePath);

        Services
            .AddSingleton<IRouteMatcher, RouteMatcher>()
            .AddSingleton<IRouteTable, RouteTable>()
            .AddNotificationHandlersFrom<UpdateRouteTable>()
            .AddHttpContextAccessor();
    }
}