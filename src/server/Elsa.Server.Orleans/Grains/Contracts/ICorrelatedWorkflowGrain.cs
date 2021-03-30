using System.Threading;
using System.Threading.Tasks;
using Elsa.Dispatch;
using Orleans;

namespace Elsa.Server.Orleans.Grains.Contracts
{
    public interface ICorrelatedWorkflowGrain : IGrainWithStringKey
    {
        Task ExecutedCorrelatedWorkflowAsync(ExecuteCorrelatedWorkflowRequest request, CancellationToken cancellationToken = default);
    }
}