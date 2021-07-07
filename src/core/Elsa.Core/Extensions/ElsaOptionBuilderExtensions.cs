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
            var enabledFeatures = new List<string>();

            foreach (var feature in features)
            {
                var featureOptions = ParseFeatureFlag(configuration, feature.Key);
                if (!featureOptions.Enabled) continue;

                var key = feature.Key.Replace($"{elsaFeaturesSection}:", string.Empty);
                enabledFeatures.Add(key);

                var hasFramework = !string.IsNullOrWhiteSpace(featureOptions.Framework);
                var hasConnection = !string.IsNullOrWhiteSpace(featureOptions.ConnectionStringIdentifier);

                if (hasFramework)
                {
                    enabledFeatures.Add($"{key}:{featureOptions.Framework}");
                }

                if (hasConnection)
                {
                    enabledFeatures.Add(hasFramework 
                        ? $"{key}:{featureOptions.Framework}:{featureOptions.ConnectionStringIdentifier}" 
                        : $"{key}:{featureOptions.ConnectionStringIdentifier}");
                }                
            }

            return enabledFeatures;
        }

        private static FeatureOptions ParseFeatureFlag(IConfiguration configuration, string feature)
        {
            var options = new FeatureOptions();

            if (configuration.GetSection($"{feature}:Enabled").Exists())
            {                
                configuration.GetSection(feature).Bind(options);
                return options;
            }

            if (!feature.EndsWith(":Enabled"))
            {
                bool.TryParse(configuration.GetValue<string>($"{feature}"), out var enabled);
                options.Enabled = enabled;
            }
            return options;
        }

        private static IEnumerable<Assembly> GetAssemblies(IEnumerable<Type> assemblyMarkerTypes) => assemblyMarkerTypes.Select(x => x.Assembly).Distinct();
    }
}