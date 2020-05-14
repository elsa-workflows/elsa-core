using System;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Extensions
{
    public static class AutoMapperServiceCollectionExtensions
    {
        public static IServiceCollection AddAutoMapper(this IServiceCollection services, ServiceLifetime lifetime)
        {
            services.TryAddSingleton(CreateMapper);
            return services;
        }

        public static IServiceCollection AddAutoMapperProfile<TProfile>(this IServiceCollection services, ServiceLifetime lifetime) where TProfile : MapperProfile
        {
            services.AddAutoMapper(lifetime);
            services.TryAddProvider<MapperProfile, TProfile>(lifetime);
            return services;
        }

        private static IMapper CreateMapper(IServiceProvider services)
        {
            var profiles = services.GetServices<MapperProfile>();
            return new AutoMapperMapper(profiles);
        }
    }
}