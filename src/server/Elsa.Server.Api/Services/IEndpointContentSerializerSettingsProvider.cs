using Newtonsoft.Json;

namespace Elsa.Server.Api.Services
{
    public interface IEndpointContentSerializerSettingsProvider
    {
        JsonSerializerSettings GetSettings();
    }
}