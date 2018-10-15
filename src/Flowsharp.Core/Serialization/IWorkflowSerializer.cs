using Flowsharp.Models;
using Newtonsoft.Json.Linq;

namespace Flowsharp.Services
{
    public interface IWorkflowSerializer
    {
        string Serialize(Workflow workflow);
        Workflow Deserialize(string data);
    }
}