using Elsa.Serialization.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Server.Api.Services
{
    public class EndpointContentSerializerSettingsProvider : IEndpointContentSerializerSettingsProvider
    {
        private readonly JsonSerializerSettings _serializerSettings;
        
        public EndpointContentSerializerSettingsProvider()
        {
            _serializerSettings = new JsonSerializerSettings();
            _serializerSettings.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            _serializerSettings.NullValueHandling = NullValueHandling.Ignore;
            _serializerSettings.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;
            _serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy
                {
                    ProcessDictionaryKeys = false,
                    ProcessExtensionDataNames = true,
                    OverrideSpecifiedNames = false
                }
            };
            _serializerSettings.Converters.Add(new FlagEnumConverter(new DefaultNamingStrategy()));
        }

        public JsonSerializerSettings GetSettings() => _serializerSettings;
    }
}