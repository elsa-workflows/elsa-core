using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Orleans;

namespace Elsa.Server.Orleans.Grains.Contracts
{
    public interface IWorkflowDefinitionGrain : IGrainWithStringKey
    {
        Task ExecuteWorkflowAsync(ExecuteWorkflowDefinitionRequest request, CancellationToken cancellationToken = default);
    }
}