
using Flowsharp.Activities;
using Flowsharp.Handlers;
using OrchardCore.DisplayManagement.Handlers;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
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