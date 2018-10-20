using Flowsharp.Models;

namespace Flowsharp.Serialization
{
    public interface IWorkflowSerializer
    {
        string Serialize(Workflow workflow);
        Workflow Deserialize(string data);
    }
}