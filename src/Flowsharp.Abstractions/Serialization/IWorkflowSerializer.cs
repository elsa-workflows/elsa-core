using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities;

namespace Flowsharp.Services
{
    public interface IWorkflowSerializer
    {
        Task<string> SerializeAsync(Workflow workflow, CancellationToken cancellationToken);
    }
}