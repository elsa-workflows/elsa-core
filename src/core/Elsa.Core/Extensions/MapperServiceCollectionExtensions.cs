using System;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Extensions
{
    public static class MapperServiceCollectionExtensions
    {
        public static IServiceCollection AddMapper(this IServiceCollection services, ServiceLifetime lifetime)
        {
            services.TryAddSingleton(CreateMapper);
            return services;
        }

        public static IServiceCollection AddMapperProfile<TProfile>(this IServiceCollection services, ServiceLifetime lifetime) where TProfile : MappingProfile
        {
            services.AddMapper(lifetime);
            services.TryAddProvider<MappingProfile, TProfile>(lifetime);
            return services;
        }

        private static IMapper CreateMapper(IServiceProvider services)
        {
            var profiles = services.GetServices<MappingProfile>();
            return new AutoMapperMapper(profiles);
        }
    }
}