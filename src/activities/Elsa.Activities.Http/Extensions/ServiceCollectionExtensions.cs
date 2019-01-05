using Elsa.Activities.Http.Descriptors;
using Elsa.Activities.Http.Drivers;
using Elsa.Activities.Http.Initialization;
using Elsa.Activities.Http.Services;
using Elsa.Activities.Http.Services.Implementations;
using Elsa.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Activities.Http.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHttpDescriptors(this IServiceCollection services)
        {
            return services.AddActivityDescriptor<HttpRequestTriggerDescriptor>();
        }
        
        public static IServiceCollection AddHttpDrivers(this IServiceCollection services)
        {
            services
                .AddSingleton<IHttpWorkflowCache, DefaultHttpWorkflowCache>()
                .AddAsyncInitializer<HttpWorkflowCacheInitializer>()
                .AddActivityDriver<HttpRequestTriggerDriver>();

            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            return services;
        }
    }
}