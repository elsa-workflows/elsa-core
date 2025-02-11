using Elsa.Workflows.Attributes;

namespace Elsa.Workflows.Runtime.Stimuli;

/// <summary>
/// A bookmark that is used to resume a workflow that is waiting for a background activity to complete.
/// </summary>
public class BackgroundActivityStimulus
{
    /// <summary>
    /// Set retroactively after the job has been scheduled. It is used to cancel the job when the bookmark is deleted.
    /// </summary>
    [ExcludeFromHash]public string? JobId { get; set; }
}