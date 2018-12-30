using Flowsharp.Activities.Http.Handlers;
using Flowsharp.Activities.Http.Initialization;
using Flowsharp.Activities.Http.Services;
using Flowsharp.Activities.Http.Services.Implementations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Flowsharp.Activities.Http.Extensions
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