using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;

namespace Elsa.Services
{
    public interface IWorkflowInstanceManager
    {
        IWorkflowInstanceStore Store { get; }
        Task SaveAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default);
        Task DeleteAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default);
        Task<WorkflowInstance?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    }
}