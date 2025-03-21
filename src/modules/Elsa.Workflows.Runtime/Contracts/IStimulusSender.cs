using Elsa.Workflows.Runtime.Results;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Represents a broker for delivering stimuli to activities in a workflow.
/// </summary>
public interface IStimulusSender
{
    /// <summary>
    /// Delivers a stimulus to an activity. This could result in new workflow instances as well as existing workflow instances being resumed.
    /// </summary>
    Task<SendStimulusResult> SendAsync(string activityTypeName, object stimulus, StimulusMetadata? metadata = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delivers a stimulus to an activity. This could result in new workflow instances as well as existing workflow instances being resumed.
    /// </summary>
    Task<SendStimulusResult> SendAsync(string stimulusHash, StimulusMetadata? metadata = null, CancellationToken cancellationToken = default);
}