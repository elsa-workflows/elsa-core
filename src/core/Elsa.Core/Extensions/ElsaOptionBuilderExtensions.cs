using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Elsa.Attributes;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;

namespace Elsa
{
    public static class ElsaOptionBuilderExtensions
    {
        public static ElsaOptionsBuilder AddFeatures(this ElsaOptionsBuilder builder, IEnumerable<Type> assemblyMarkerTypes, IConfiguration configuration) => AddFeatures(builder, GetAssemblies(assemblyMarkerTypes), configuration);

        public static ElsaOptionsBuilder AddFeatures(this ElsaOptionsBuilder builder, IEnumerable<Assembly> assemblies, IConfiguration configuration)
        {
            var enabledFeatures = builder.ElsaOptions.FeatureOptions.Features.ToHashSet();

            var startupTypesQuery = from assembly in assemblies
                from type in assembly.GetExportedTypes()
                where type.IsClass && !type.IsAbstract && typeof(IStartup).IsAssignableFrom(type)
                let featureAttribute = type.GetCustomAttribute<FeatureAttribute>()
                let enabledFeature = enabledFeatures.FirstOrDefault(x => x.Key == featureAttribute.FeatureName && x.Value)
                where featureAttribute != null && !enabledFeature.Equals(default(KeyValuePair<string, bool>))
                select type;

            var startupTypes = startupTypesQuery.ToList();

            foreach (var type in startupTypes)
            {
                var instance = (IStartup) Activator.CreateInstance(type, null);
                instance.ConfigureElsa(builder, configuration);
                builder.ElsaOptions.Startups.Add(instance);
            }

            return builder;
        }
        
        private static IEnumerable<Assembly> GetAssemblies(IEnumerable<Type> assemblyMarkerTypes) => assemblyMarkerTypes.Select(x => x.Assembly).Distinct();
    }
}