using Elsa.Activities.Http.Drivers;
using Elsa.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Activities.Http.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHttpWorkflowDescriptors(this IServiceCollection services)
        {
            return services.AddActivityDescriptors<ActivityDescriptors>();
        }

        public static IServiceCollection AddHttpWorkflowDrivers(this IServiceCollection services)
        {
            services.AddHttpWorkflowDescriptors();
            services.AddAsyncInitialization();
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            
            services
                .AddActivityDriver<HttpRequestTriggerDriver>()
                .AddActivityDriver<HttpResponseActionDriver>();
            
            return services;
        }
    }
}