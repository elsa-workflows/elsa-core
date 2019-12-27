using Newtonsoft.Json;

namespace Elsa.Serialization.Extensions
{
    public static class JsonSerializerSettingsExtensions
    {
        public static JsonSerializerSettings WithConverter(this JsonSerializerSettings settings, JsonConverter converter)
        {
            settings.Converters.Add(converter);
            return settings;
        }
    }
}