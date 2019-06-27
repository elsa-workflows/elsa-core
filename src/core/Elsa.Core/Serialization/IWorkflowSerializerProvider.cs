using Newtonsoft.Json;

namespace Elsa.Core.Serialization
{
    public interface IWorkflowSerializerProvider
    {
        JsonSerializer CreateJsonSerializer();
    }
}