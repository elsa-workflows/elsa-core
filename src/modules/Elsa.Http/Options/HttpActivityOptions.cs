using System;
using Elsa.Http.Handlers;
using Elsa.Http.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Http.Options
{
    public class HttpActivityOptions
    {
        /// <summary>
        /// The root path at which HTTP activities can be invoked.
        /// </summary>
        public PathString? BasePath { get; set; }

        public Func<IServiceProvider, IHttpEndpointAuthorizationHandler> HttpEndpointAuthorizationHandlerFactory { get; set; } = ActivatorUtilities.GetServiceOrCreateInstance<AllowAnonymousHttpEndpointAuthorizationHandler>;
        public Func<IServiceProvider, IHttpEndpointWorkflowFaultHandler> HttpEndpointWorkflowFaultHandlerFactory { get; set; } = ActivatorUtilities.GetServiceOrCreateInstance<DefaultHttpEndpointWorkflowFaultHandler>;
    }
}