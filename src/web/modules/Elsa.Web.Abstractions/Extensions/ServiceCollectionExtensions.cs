using Microsoft.Extensions.DependencyInjection;
using OrchardCore.DisplayManagement.Handlers;

namespace Elsa.Web.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddActivityDisplay<TDisplayDriver>(this IServiceCollection services) 
            where TDisplayDriver : class, IDisplayDriver<IActivity>
        {
            return services
                .AddScoped<IDisplayDriver<IActivity>, TDisplayDriver>();
        }
    }
}