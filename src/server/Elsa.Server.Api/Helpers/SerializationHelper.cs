using Elsa.Serialization;
using Elsa.Server.Api.Converters;
using Newtonsoft.Json;

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
    }
}