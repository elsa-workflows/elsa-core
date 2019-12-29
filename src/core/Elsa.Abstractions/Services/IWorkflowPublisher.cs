using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Services
{
    public interface IWorkflowPublisher
    {
        ProcessDefinitionVersion New();
        
        Task<ProcessDefinitionVersion> PublishAsync(string id, CancellationToken cancellationToken);

        Task<ProcessDefinitionVersion> PublishAsync(
            ProcessDefinitionVersion processDefinition,
            CancellationToken cancellationToken);

        Task<ProcessDefinitionVersion> GetDraftAsync(string id, CancellationToken cancellationToken);

        Task<ProcessDefinitionVersion> SaveDraftAsync(
            ProcessDefinitionVersion processDefinition,
            CancellationToken cancellationToken);
    }
}