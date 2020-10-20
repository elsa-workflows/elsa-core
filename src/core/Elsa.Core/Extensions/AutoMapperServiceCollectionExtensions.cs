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
            services.TryAdd(new ServiceDescriptor(typeof(IMapper), sp => sp.GetRequiredService<IConfigurationProvider>().CreateMapper(sp.GetService), lifetime));
            return services;
        }

        public static IServiceCollection AddAutoMapperProfile<TProfile>(this IServiceCollection services) where TProfile : Profile
        {
            services.TryAddProvider<Profile, TProfile>(ServiceLifetime.Transient);
            return services;
        }

        public static IConfigurationProvider CreateAutoMapperConfiguration(this IServiceProvider serviceProvider)
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