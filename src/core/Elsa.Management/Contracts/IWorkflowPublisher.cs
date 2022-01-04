using Elsa.Models;

namespace Elsa.Management.Contracts
{
    public interface IWorkflowPublisher
    {
        Workflow New();
        Task<Workflow?> PublishAsync(string definitionId, CancellationToken cancellationToken = default);
        Task<Workflow> PublishAsync(Workflow workflow, CancellationToken cancellationToken = default);
        Task<Workflow?> RetractAsync(string definitionId, CancellationToken cancellationToken = default);
        Task<Workflow> RetractAsync(Workflow workflow, CancellationToken cancellationToken = default);
        Task<Workflow?> GetDraftAsync(string definitionId, CancellationToken cancellationToken= default);
        Task<Workflow> SaveDraftAsync(Workflow workflow, CancellationToken cancellationToken = default);
        Task DeleteAsync(string definitionId, CancellationToken cancellationToken = default);
        Task DeleteAsync(Workflow workflow, CancellationToken cancellationToken = default);
    }
}