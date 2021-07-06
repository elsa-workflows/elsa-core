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
            var enabledFeatures = ParseFeatures(configuration);

            if (enabledFeatures == null!) // Null when configuration binding finds an empty array.
                return builder;

            enabledFeatures = enabledFeatures.ToHashSet();

            var startupTypesQuery = from assembly in assemblies
                from type in assembly.GetExportedTypes()
                where type.IsClass && !type.IsAbstract && typeof(IStartup).IsAssignableFrom(type)
                let featureAttribute = type.GetCustomAttribute<FeatureAttribute>()
                where featureAttribute != null && enabledFeatures.Contains(featureAttribute.FeatureName)
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

        private static IEnumerable<string> ParseFeatures(IConfiguration configuration)
        {
            var elsaFeaturesSection = "Elsa:Features";
            var features = configuration.GetSection(elsaFeaturesSection).AsEnumerable();

            return
                from feature in features
                let isEnabled = ParseFeatureFlag(configuration, feature.Key)
                where isEnabled
                select feature.Key.Replace($"{elsaFeaturesSection}:", string.Empty);
        }

        private static bool ParseFeatureFlag(IConfiguration configuration, string feature)
        {
            if (configuration.GetSection($"{feature}:Enabled").Exists())
            {
                bool.TryParse(configuration.GetValue<string>($"{feature}:Enabled"), out var enabled);
                return enabled;
            }

            if (!feature.EndsWith(":Enabled"))
            {
                bool.TryParse(configuration.GetValue<string>($"{feature}"), out var enabled);
                return enabled;
            }

            return false;
        }

        private static IEnumerable<Assembly> GetAssemblies(IEnumerable<Type> assemblyMarkerTypes) => assemblyMarkerTypes.Select(x => x.Assembly).Distinct();
    }
}