using System;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.AutoMapper.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAutoMapper(this IServiceCollection services, ServiceLifetime lifetime)
        {
            services.TryAddSingleton(CreateConfigurationProvider);
            services.TryAdd(new ServiceDescriptor(typeof(IMapper), sp => new Mapper(sp.GetRequiredService<IConfigurationProvider>(), sp.GetService), lifetime));

            return services;
        }

        public static IServiceCollection AddAutoMapperProfile<TProfile>(this IServiceCollection services, ServiceLifetime lifetime) where TProfile : Profile
        {
            services.AddAutoMapper(lifetime);
            return services.AddTransient<Profile, TProfile>();
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