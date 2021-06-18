using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Client.Models;
using Refit;

namespace Elsa.Activities.Telnyx.Client.Services
{
    public interface ICallsApi
    {
        [Post("/v2/calls")]
        Task<TelnyxResponse<DialResponse>> DialAsync([Body]DialRequest request, CancellationToken cancellationToken = default);
        
        [Post("/v2/calls/{callControlId}/actions/answer")]
        Task AnswerCallAsync(string callControlId, [Body]AnswerCallRequest request, CancellationToken cancellationToken = default);
        
        [Post("/v2/calls/{callControlId}/actions/transfer")]
        Task TransferCallAsync(string callControlId, [Body]TransferCallRequest request, CancellationToken cancellationToken = default);
        
        [Post("/v2/calls/{callControlId}/actions/hangup")]
        Task HangupCallAsync(string callControlId, [Body]HangupCallRequest request, CancellationToken cancellationToken = default);
        
        [Post("/v2/calls/{callControlId}/actions/gather_using_audio")]
        Task GatherUsingAudioAsync(string callControlId, [Body]GatherUsingAudioRequest request, CancellationToken cancellationToken = default);
        
        [Post("/v2/calls/{callControlId}/actions/gather_using_speak")]
        Task GatherUsingSpeakAsync(string callControlId, [Body]GatherUsingSpeakRequest request, CancellationToken cancellationToken = default);
        
        [Post("/v2/calls/{callControlId}/actions/bridge")]
        Task BridgeCallsAsync(string callControlId, [Body]BridgeCallsRequest request, CancellationToken cancellationToken = default);
        
        [Post("/v2/calls/{callControlId}/actions/playback_start")]
        Task PlayAudioAsync(string callControlId, [Body]PlayAudioRequest request, CancellationToken cancellationToken = default);
        
        [Post("/v2/calls/{callControlId}/actions/playback_stop")]
        Task StopAudioPlaybackAsync(string callControlId, [Body]StopAudioPlaybackRequest request, CancellationToken cancellationToken = default);
        
        [Post("/v2/calls/{callControlId}/actions/record_start")]
        Task StartRecordingAsync(string callControlId, [Body]StartRecordingRequest request, CancellationToken cancellationToken = default);
        
        [Post("/v2/calls/{callControlId}/actions/record_stop")]
        Task StopRecordingAsync(string callControlId, [Body]StopRecordingRequest request, CancellationToken cancellationToken = default);
        
        [Post("/v2/calls/{callControlId}/actions/speak")]
        Task SpeakTextAsync(string callControlId, [Body]SpeakTextRequest request, CancellationToken cancellationToken = default);
    }
}