using Elsa.Server.Api.Helpers;
using Newtonsoft.Json;

namespace Elsa.Server.Api.Services
{
    public class EndpointContentSerializerSettingsProvider : IEndpointContentSerializerSettingsProvider
    {
        private readonly JsonSerializerSettings _serializerSettings;
        public EndpointContentSerializerSettingsProvider() => _serializerSettings = SerializationHelper.GetSettingsForEndpoint();
        public JsonSerializerSettings GetSettings() => _serializerSettings;
    }
}