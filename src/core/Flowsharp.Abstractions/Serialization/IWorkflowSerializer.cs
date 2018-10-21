using Flowsharp.Models;
using Newtonsoft.Json.Linq;

namespace Flowsharp.Serialization
{
    public interface IWorkflowSerializer
    {
        string Serialize(Workflow workflow);
        string Serialize(JToken  token);
        Workflow Deserialize(string data);
        Workflow Deserialize(JToken token);
    }
}