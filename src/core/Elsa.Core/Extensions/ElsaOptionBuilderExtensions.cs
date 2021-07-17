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

        /// <summary>
        /// Parse all features from the configuration, filter only enabled features, 
        /// find all start up classes with matching attribute and create their instances.
        /// </summary>
        public static ElsaOptionsBuilder AddFeatures(this ElsaOptionsBuilder builder, IEnumerable<Assembly> assemblies, IConfiguration configuration)
        {
            var enabledFeatures = ParseFeatures(configuration);

            if (enabledFeatures == null!)
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

        /// <summary>
        /// Parse all features from the configuration and populate enabled feature collection.
        /// </summary>
        private static IEnumerable<string> ParseFeatures(IConfiguration configuration)
        {
            var elsaFeaturesSection = "Elsa:Features";
            var features = configuration.GetSection(elsaFeaturesSection).AsEnumerable();
            var enabledFeatures = new List<string>();

            foreach (var feature in features)
            {
                var featureOptions = ParseFeatureSection(configuration, feature.Key);
                if (!featureOptions.Enabled) continue;

                var key = feature.Key.Replace($"{elsaFeaturesSection}:", string.Empty);
                enabledFeatures.Add(key);

                if (featureOptions.Items == null) continue;

                var values = featureOptions.Items.Values.ToArray();
                GetPermutations(key, values, enabledFeatures, 0, values.Length - 1);                
            }

            return enabledFeatures;
        }

        /// <summary>
        /// Parse single feature section from the configuration and populate feature model.
        /// </summary>
        private static FeatureModel ParseFeatureSection(IConfiguration configuration, string feature)
        {
            var featureModel = new FeatureModel();

            if (configuration.GetSection($"{feature}:Enabled").Exists())
            {
                var featureItems = configuration.GetSection($"{feature}").AsEnumerable();

                ParseFeatureItems(feature, featureModel, featureItems);
                ParseFeatureOptions(configuration, feature, featureModel);

                return featureModel;
            }

            if (!feature.EndsWith(":Enabled"))
            {
                bool.TryParse(configuration.GetValue<string>($"{feature}"), out var enabled);
                featureModel.Enabled = enabled;
            }
            return featureModel;
        }

        /// <summary>
        /// Parse feature section key/value collection except Enabled and Options keys from the configuration and populate feature model.
        /// </summary>
        private static void ParseFeatureItems(string feature, FeatureModel featureModel, IEnumerable<KeyValuePair<string, string>> featureItems)
        {
            featureModel.Items = new Dictionary<string, string>();

            foreach (var featureItem in featureItems)
            {
                if (featureItem.Value == null) continue;

                var itemKey = featureItem.Key.Replace($"{feature}:", string.Empty);

                if (itemKey.Contains(":")) continue;

                if (itemKey == "Enabled")
                {
                    bool.TryParse(featureItem.Value, out var enabled);
                    featureModel.Enabled = enabled;
                }
                else
                {
                    featureModel.Items.Add(itemKey, featureItem.Value);
                }
            }
        }

        /// <summary>
        /// Parse feature Options section from the configuration and populate feature model.
        /// </summary>
        private static void ParseFeatureOptions(IConfiguration configuration, string feature, FeatureModel featureModel)
        {
            if (configuration.GetSection($"{feature}:Options").Exists())
            {
                var options = new Dictionary<string, string>();
                configuration.GetSection($"{feature}:Options").Bind(options);
                featureModel.Options = options;
            }
        }

        /// <summary>
        /// Permutate all possible order combinations in Feature section key/value collection.
        /// </summary>
        private static void GetPermutations(string feature, string[] values, ICollection<string> enabledFeatures, int start, int end)
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
                GetPermutations(feature, values, enabledFeatures, start + 1, end);
                Swap(ref values[start], ref values[i]);
            }
        }

        /// <summary>
        /// Swap two string values.
        /// </summary>
        private static void Swap(ref string item1, ref string item2)
        {
            if (item1 == item2) return;

            var temp = item1;
            item1 = item2;
            item2 = temp;
        }

        private static IEnumerable<Assembly> GetAssemblies(IEnumerable<Type> assemblyMarkerTypes) => assemblyMarkerTypes.Select(x => x.Assembly).Distinct();

        private class FeatureModel
        {
            public bool Enabled { get; set; }
            public Dictionary<string, string>? Items { get; set; }
            public Dictionary<string, string>? Options { get; set; }
        }
    }
}