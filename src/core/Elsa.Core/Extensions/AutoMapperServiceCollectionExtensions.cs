using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public static IServiceCollection AddAutoMapperProfiles(this IServiceCollection services, IEnumerable<Assembly> assemblies) =>
            services.Scan(scan => scan.FromAssemblies(assemblies).AddClasses(classes => classes.AssignableTo<Profile>()).As<Profile>().WithTransientLifetime());

        public static IServiceCollection AddAutoMapperProfiles(this IServiceCollection services, params Assembly[] assemblies) => services.AddAutoMapperProfiles(assemblies.AsEnumerable());
        public static IServiceCollection AddAutoMapperProfiles(this IServiceCollection services, Assembly assembly) => AddAutoMapperProfiles(services, new[] { assembly });
        public static IServiceCollection AddAutoMapperProfiles<TAssemblyMarkerType>(this IServiceCollection services) => services.AddAutoMapperProfiles(typeof(TAssemblyMarkerType).Assembly);

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