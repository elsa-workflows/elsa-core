using Flowsharp.Models;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.DisplayManagement.Handlers;

// ReSharper disable once CheckNamespace
namespace Flowsharp.Web.Abstractions.Extensions
{
    namespace Flowsharp.Web.Abstractions.Extensions
    {
        public static class ServiceCollectionExtensions
        {
            public static IServiceCollection AddActivity<THandler, TDriver>(this IServiceCollection services) 
                where THandler : class, IActivityHandler 
                where TDriver : class, IDisplayDriver<IActivity>
            {
                return services
                    .AddScoped<IActivityHandler, THandler>()
                    .AddScoped<IDisplayDriver<IActivity>, TDriver>();
            }
        }
    }
}