using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Newtonsoft.Json.Linq;

namespace Flowsharp.Serialization
{
    public interface IWorkflowSerializer
    {
        Task<string> SerializeAsync(Workflow workflow, string format, CancellationToken cancellationToken);
        Task<string> SerializeAsync(JToken  token, string format, CancellationToken cancellationToken);
        Task<Workflow> DeserializeAsync(string data, string format, CancellationToken cancellationToken);
        Task<Workflow> DeserializeAsync(JToken token, CancellationToken cancellationToken);
        Task<Workflow> CloneAsync(Workflow workflow, CancellationToken cancellationToken);
        Task<Workflow> DeriveAsync(Workflow parent, CancellationToken cancellationToken);
    }
}