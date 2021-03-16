using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Client.Models;
using Refit;

namespace Elsa.Activities.Telnyx.Client.Services
{
    public interface ICallsApi
    {
        [Post("/v2/calls/{callControlId}/actions/answer")]
        Task AnswerCallAsync(string callControlId, [Body]AnswerCallRequest request, CancellationToken cancellationToken = default);
        
        [Post("/v2/calls/{callControlId}/actions/gather_using_audio")]
        Task GatherUsingAudiAsync(string callControlId, [Body]GatherUsingAudioRequest request, CancellationToken cancellationToken = default);
    }
}