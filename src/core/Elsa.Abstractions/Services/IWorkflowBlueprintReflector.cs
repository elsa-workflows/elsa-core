using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowBlueprintReflector
    {
        public Task<IWorkflowBlueprintWrapper> ReflectAsync(IWorkflowBlueprint workflowBlueprint, CancellationToken cancellationToken = default);
    }
}