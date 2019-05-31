using Elsa.Activities.Http.Drivers;
using Elsa.Activities.Http.Formatters;
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

        public static IServiceCollection AddHttpActivities(this IServiceCollection services)
        {
            services.AddHttpWorkflowDescriptors();
            services.AddAsyncInitialization();
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddHttpClient(nameof(HttpRequestActionDriver));
            
            services
                .AddActivityDriver<HttpRequestTriggerDriver>()
                .AddActivityDriver<HttpResponseActionDriver>()
                .AddActivityDriver<HttpRequestActionDriver>();

            services
                .AddSingleton<IContentFormatter, NullContentFormatter>()
                .AddSingleton<IContentFormatter, JsonContentFormatter>();
            
            return services;
        }
    }
}