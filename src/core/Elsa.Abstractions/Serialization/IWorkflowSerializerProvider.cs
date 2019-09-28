using Newtonsoft.Json;

namespace Elsa.Serialization
{
    public interface IWorkflowSerializerProvider
    {
        JsonSerializerSettings CreateJsonSerializerSettings();
        JsonSerializer CreateJsonSerializer();
    }
}