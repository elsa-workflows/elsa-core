using Elsa.Serialization;
using Elsa.Serialization.Converters;
using Elsa.Server.Api.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Server.Api.Helpers
{
    public static class SerializationHelper
    {
        public static JsonSerializerSettings GetSettingsForWorkflowDefinition()
        {
            // Here we don't want to use the `PreserveReferencesHandling` setting because the model will be used by the designer "as-is" and will not resolve $id references.
            // Fixes #1605.
            var settings = DefaultContentSerializer.CreateDefaultJsonSerializationSettings();
            settings.PreserveReferencesHandling = PreserveReferencesHandling.None;
            settings.Converters.Add(new VariablesConverter());

            return settings;
        }

        public static JsonSerializerSettings GetSettingsForEndpoint() => GetSettingsForEndpoint(new JsonSerializerSettings()); 
        
        public static JsonSerializerSettings GetSettingsForEndpoint(JsonSerializerSettings settings)
        {
            settings.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            settings.NullValueHandling = NullValueHandling.Ignore;
            settings.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy
                {
                    ProcessDictionaryKeys = false,
                    ProcessExtensionDataNames = true,
                    OverrideSpecifiedNames = false
                }
            };
            settings.Converters.Add(new FlagEnumConverter(new DefaultNamingStrategy()));
            return settings;
        }
    }
}