using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Elsa.Activities.Dropbox.Extensions
{
    public static class JsonSerializationSettingsExtensions
    {
        public static JsonSerializerSettings ConfigureForDropboxApi(this JsonSerializerSettings settings)
        {
            settings.ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            };
            settings.NullValueHandling = NullValueHandling.Ignore;
            settings.Converters.Add(new StringEnumConverter(new SnakeCaseNamingStrategy()));

            return settings;
        }
    }
}