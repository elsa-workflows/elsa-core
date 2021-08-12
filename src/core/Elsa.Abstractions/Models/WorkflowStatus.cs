using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Elsa.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum WorkflowStatus
    {
        Idle,
        Running,
        Finished,
        Suspended,
        Faulted,
        Cancelled
    }
}
