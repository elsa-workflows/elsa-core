using Elsa.Activities.Http.Activities;
using Elsa.Activities.Http.Formatters;
using Elsa.Core.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Activities.Http.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHttpActivities(this IServiceCollection services)
        {
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddHttpClient(nameof(HttpRequestAction));
            
            services
                .AddActivity<HttpRequestTrigger>()
                .AddActivity<HttpResponseAction>()
                .AddActivity<HttpRequestAction>();

            services
                .AddSingleton<IContentFormatter, NullContentFormatter>()
                .AddSingleton<IContentFormatter, JsonContentFormatter>();
            
            return services;
        }
    }
}