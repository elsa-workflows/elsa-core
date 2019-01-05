using Microsoft.Extensions.DependencyInjection;
using OrchardCore.DisplayManagement.Handlers;

namespace Elsa.Web.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddActivity<TDescriptor, TDisplayDriver>(this IServiceCollection services) 
            where TDescriptor : class, IActivityDescriptor
            where TDisplayDriver : class, IDisplayDriver<IActivity>
        {
            return services
                .AddScoped<IActivityDescriptor, TDescriptor>()
                .AddScoped<IDisplayDriver<IActivity>, TDisplayDriver>();
        }
    }
}