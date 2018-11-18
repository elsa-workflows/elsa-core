using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Newtonsoft.Json.Linq;

namespace Flowsharp.Serialization
{
    public interface IWorkflowSerializer
    {
        Task<string> SerializeAsync(Workflow workflow, CancellationToken cancellationToken);
        Task<string> SerializeAsync(JToken  token, CancellationToken cancellationToken);
        Task<Workflow> DeserializeAsync(string data, CancellationToken cancellationToken);
        Task<Workflow> DeserializeAsync(JToken token, CancellationToken cancellationToken);
    }
}