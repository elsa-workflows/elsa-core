using Elsa.Models;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization
{
    public interface IWorkflowSerializer
    {
        string Serialize(Workflow workflow, string format);
        string Serialize(JToken  token, string format);
        Workflow Deserialize(string data, string format);
        Workflow Deserialize(JToken token);
        Workflow Clone(Workflow workflow);
        Workflow Derive(Workflow parent);
    }
}