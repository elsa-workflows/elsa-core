using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;

namespace Elsa.Workflows.Runtime;

public interface IStimulusDispatcher
{
    /// <summary>
    /// Dispatches a stimulus to an activity. This could result in new workflow instances as well as existing workflow instances being resumed.
    /// </summary>
    Task<DispatchStimulusResponse> SendAsync(DispatchStimulusRequest request, CancellationToken cancellationToken = default);
}