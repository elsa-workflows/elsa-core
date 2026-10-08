namespace Elsa.Api.Client.Resources.Alterations.Models;

/// <summary>
/// The result of running a series of alterations.
/// </summary>
public class RunAlterationsResult
{
    /// <summary>
    /// The ID of the workflow instance that was altered.
    /// </summary>
    public string WorkflowInstanceId { get; set; } = string.Empty;

    /// <summary>
    /// A log of the alterations that were run.
    /// </summary>
    public AlterationLog Log { get; set; } = new();

    /// <summary>
    /// A flag indicating whether the workflow has scheduled work.
    /// </summary>
    public bool WorkflowHasScheduledWork { get; set; }

    /// <summary>
    /// A flag indicating whether the alterations have succeeded.
    /// </summary>
    public bool IsSuccessful { get; set; }
}