using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Dispatch;
using Orleans;

namespace Elsa.Server.Orleans.Grains.Contracts
{
    public interface ICorrelatedWorkflowGrain : IGrainWithStringKey
    {
        Task ExecutedCorrelatedWorkflowAsync(TriggerWorkflowsRequest request, CancellationToken cancellationToken = default);
    }
}