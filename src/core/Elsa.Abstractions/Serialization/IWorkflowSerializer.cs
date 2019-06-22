using Elsa.Serialization.Models;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization
{
    public interface IWorkflowSerializer
    {
        string Serialize(WorkflowInstance workflowInstance, string format);
        string Serialize(JToken  token, string format);
        WorkflowInstance Deserialize(string data, string format);
        WorkflowInstance Deserialize(JToken token);
    }
}