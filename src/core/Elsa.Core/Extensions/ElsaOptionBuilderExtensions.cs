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

                if (featureOptions.Items == null) continue;

                //Permutations = new List<string>();
                var values = featureOptions.Items.Values.ToArray();
                GetPermutations(key, enabledFeatures, values, 0, values.Length - 1);                
            }

            return enabledFeatures;
        }

        private static void GetPermutations(string feature, ICollection<string> enabledFeatures, string[] values, int start, int end)
        {
            if (start == end)
            {
                foreach (var item in values)
                {
                    feature += $":{item}";
                    if (enabledFeatures.Contains(feature)) continue;
                    enabledFeatures.Add(feature);
                }
            }

            for (int i = start; i <= end; i++)
            {
                Swap(ref values[start], ref values[i]);
                GetPermutations(feature, enabledFeatures, values, start + 1, end);
                Swap(ref values[start], ref values[i]);
            }
        }

        private static void Swap(ref string item1, ref string item2)
        {
            if (item1 == item2) return;

            var temp = item1;
            item1 = item2;
            item2 = temp;
        }

        private static FeatureOptions ParseFeatureFlag(IConfiguration configuration, string feature)
        {
            var featureOptions = new FeatureOptions();

            if (configuration.GetSection($"{feature}:Enabled").Exists())
            {
                var featureItems = configuration.GetSection($"{feature}").AsEnumerable();

                ParseFeatureItems(feature, featureOptions, featureItems);
                ParseFeatureOptions(configuration, feature, featureOptions);

                return featureOptions;
            }

            if (!feature.EndsWith(":Enabled"))
            {
                bool.TryParse(configuration.GetValue<string>($"{feature}"), out var enabled);
                featureOptions.Enabled = enabled;
            }
            return featureOptions;
        }

        private static void ParseFeatureItems(string feature, FeatureOptions featureOptions, IEnumerable<KeyValuePair<string, string>> featureItems)
        {
            featureOptions.Items = new Dictionary<string, string>();

            foreach (var featureItem in featureItems)
            {
                if (featureItem.Value == null) continue;

                var itemKey = featureItem.Key.Replace($"{feature}:", string.Empty);

                if (itemKey.Contains(":")) continue;

                if (itemKey == "Enabled")
                {
                    bool.TryParse(featureItem.Value, out var enabled);
                    featureOptions.Enabled = enabled;
                }
                else
                {
                    featureOptions.Items.Add(itemKey, featureItem.Value);
                }
            }
        }

        private static void ParseFeatureOptions(IConfiguration configuration, string feature, FeatureOptions featureOptions)
        {
            if (configuration.GetSection($"{feature}:Options").Exists())
            {
                var options = new Dictionary<string, string>();
                configuration.GetSection($"{feature}:Options").Bind(options);
                featureOptions.Options = options;
            }
        }

        private static IEnumerable<Assembly> GetAssemblies(IEnumerable<Type> assemblyMarkerTypes) => assemblyMarkerTypes.Select(x => x.Assembly).Distinct();
    }
}