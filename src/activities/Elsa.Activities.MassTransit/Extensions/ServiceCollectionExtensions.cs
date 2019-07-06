using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.MassTransit.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMassTransitActivities(this IServiceCollection services)
        {
//            services.AddMassTransit(
//                config =>
//                {
//                    
//                });

            return services;
        }
    }
}