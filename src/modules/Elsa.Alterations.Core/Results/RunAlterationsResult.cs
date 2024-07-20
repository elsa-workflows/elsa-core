using Elsa.Alterations.Core.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Alterations.Core.Results;

/// <summary>
/// The result of running a series of alterations.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RunAlterationsResult"/> class.
/// </remarks>
public class RunAlterationsResult(string workflowInstanceId, AlterationLog log)
{

    /// <summary>
    /// The ID of the workflow instance that was altered.
    /// </summary>
    public string WorkflowInstanceId { get; } = workflowInstanceId;

    /// <summary>
    /// A log of the alterations that were run.
    /// </summary>
    public AlterationLog Log { get; set; } = log;

    /// <summary>
    /// A flag indicating whether the workflow has scheduled work.
    /// </summary>
    public bool WorkflowHasScheduledWork { get; set; }
    
    /// <summary>
    /// A flag indicating whether the alterations have succeeded.
    /// </summary>
    public bool IsSuccessful => Log.LogEntries.Any(x => x.LogLevel <= LogLevel.Warning);
}