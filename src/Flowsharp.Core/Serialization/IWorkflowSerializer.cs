using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities;

namespace Flowsharp.Serialization
{
    public interface IWorkflowSerializer
    {
        Task<string> SerializeAsync(Workflow workflow, CancellationToken cancellationToken);
        Task<Workflow> DeserializeAsync(string json, CancellationToken cancellationToken);
    }
}