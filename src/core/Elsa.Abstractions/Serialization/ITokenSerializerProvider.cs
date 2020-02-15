using Newtonsoft.Json;

namespace Elsa.Serialization
{
    public interface ITokenSerializerProvider
    {
        JsonSerializerSettings CreateJsonSerializerSettings();
        JsonSerializer CreateJsonSerializer();
    }
}