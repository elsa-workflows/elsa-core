using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Orleans;

namespace Elsa.Server.Orleans.Grains.Contracts
{
    public interface IWorkflowInstanceGrain : IGrainWithStringKey
    {
        Task ExecuteWorkflowAsync(ExecuteWorkflowInstanceRequest request, CancellationToken cancellationToken = default);
    }
}