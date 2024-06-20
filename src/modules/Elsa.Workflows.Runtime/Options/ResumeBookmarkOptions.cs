using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Options;

/// <summary>
/// Options for resuming a workflow.
/// </summary>
[UsedImplicitly]
public class ResumeBookmarkOptions
{
    /// <summary>
    /// The input to provide to the workflow.
    /// </summary>
    public IDictionary<string, object>? Input { get; set; }
    
    /// <summary>
    /// The properties to provide to the workflow.
    /// </summary>
    public IDictionary<string, object>? Properties { get; set; }
}