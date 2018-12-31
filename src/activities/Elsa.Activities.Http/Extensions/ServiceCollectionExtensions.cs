using Elsa.Activities.Http.Handlers;
using Elsa.Activities.Http.Initialization;
using Elsa.Activities.Http.Services;
using Elsa.Activities.Http.Services.Implementations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Activities.Http.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWorkflowsHttp(this IServiceCollection services)
        {
            services
                .AddSingleton<IHttpWorkflowCache, DefaultHttpWorkflowCache>()
                .AddAsyncInitializer<HttpWorkflowCacheInitializer>()
                .AddSingleton<IActivityHandler, HttpRequestTriggerHandler>();

            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            return services;
        }
    }
}