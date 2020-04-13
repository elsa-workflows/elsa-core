using System;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Extensions
{
    public static class AutoMapperServiceCollectionExtensions
    {
        public static IServiceCollection AddAutoMapper(this IServiceCollection services, ServiceLifetime lifetime)
        {
            services.TryAddSingleton(CreateConfigurationProvider);
            services.TryAdd(new ServiceDescriptor(typeof(IMapper), sp => sp.GetRequiredService<IConfigurationProvider>().CreateMapper(sp.GetService), lifetime));

            return services;
        }

        public static IServiceCollection AddAutoMapperProfile<TProfile>(this IServiceCollection services, ServiceLifetime lifetime) where TProfile : Profile
        {
            services.AddAutoMapper(lifetime);
            services.TryAddProvider<Profile, TProfile>(lifetime);
            return services;
        }

        private static IConfigurationProvider CreateConfigurationProvider(IServiceProvider serviceProvider)
        {
            var profiles = serviceProvider.GetServices<Profile>();

            var configuration = new MapperConfiguration(
                x =>
                {
                    foreach (var profile in profiles)
                        x.AddProfile(profile);
                }
            );

            return configuration;
        }
    }
}