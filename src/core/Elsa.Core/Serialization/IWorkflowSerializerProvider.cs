using Newtonsoft.Json;

namespace Elsa.Serialization
{
    public interface IWorkflowSerializerProvider
    {
        JsonSerializer CreateJsonSerializer();
    }
}