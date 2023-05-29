using System.Collections.Generic;
using System.Reflection;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa
{
    public static class AutoMapperServiceCollectionExtensions
    {
        public static IServiceCollection AddAutoMapperProfile<TProfile>(this IServiceCollection services) where TProfile : Profile, new() =>
            services.Configure<MapperConfigurationExpression>(options => options.AddProfile<TProfile>());

        public static IServiceCollection AddAutoMapperProfiles(this IServiceCollection services, IEnumerable<Assembly> assemblies) =>
            services.Configure<MapperConfigurationExpression>(options => options.AddMaps(assemblies));

        public static IServiceCollection AddAutoMapperProfiles(this IServiceCollection services, params Assembly[] assemblies) => 
            services.Configure<MapperConfigurationExpression>(options => options.AddMaps(assemblies));

        public static IServiceCollection AddAutoMapperProfiles<TAssemblyMarkerType>(this IServiceCollection services) => 
            services.AddAutoMapperProfiles(typeof(TAssemblyMarkerType).Assembly);
    }
}