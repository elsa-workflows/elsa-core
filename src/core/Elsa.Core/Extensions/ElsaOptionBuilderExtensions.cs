using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Elsa.Attributes;
using Elsa.Models;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;

namespace Elsa
{
    public static class ElsaOptionBuilderExtensions
    {
        public static ElsaOptionsBuilder AddFeatures(this ElsaOptionsBuilder builder, IEnumerable<Type> assemblyMarkerTypes, IConfiguration configuration) => AddFeatures(builder, GetAssemblies(assemblyMarkerTypes), configuration);

        public static ElsaOptionsBuilder AddFeatures(this ElsaOptionsBuilder builder, IEnumerable<Assembly> assemblies, IConfiguration configuration)
        {
            ParseFeatures(configuration);

            if (EnabledFeatures == null!) // Null when configuration binding finds an empty array.
                return builder;
            
            var enabledFeatures = EnabledFeatures.ToHashSet();

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

        private static void ParseFeatures(IConfiguration configuration)
        {
            var elsaFeaturesSection = "Elsa:Features";

            EnabledFeatures = new List<string>();
            var features = configuration.GetSection(elsaFeaturesSection).AsEnumerable();

            foreach (var feature in features)
            {
                var explEnabled = false;
                var implEnabled = false;

                if (configuration.GetSection($"{feature.Key}:Enabled").Exists())
                {
                    bool.TryParse(configuration.GetValue<string>($"{feature.Key}:Enabled"), out explEnabled);
                }
                else if (!feature.Key.EndsWith(":Enabled"))
                {
                    bool.TryParse(configuration.GetValue<string>($"{feature.Key}"), out implEnabled);
                }

                if (!explEnabled && !implEnabled) continue;

                var featureName = feature.Key.Replace($"{elsaFeaturesSection}:", string.Empty);

                EnabledFeatures.Add(featureName);
            }
        }

        private static IEnumerable<Assembly> GetAssemblies(IEnumerable<Type> assemblyMarkerTypes) => assemblyMarkerTypes.Select(x => x.Assembly).Distinct();

        private static ICollection<string>? EnabledFeatures { get; set; }
    }
}