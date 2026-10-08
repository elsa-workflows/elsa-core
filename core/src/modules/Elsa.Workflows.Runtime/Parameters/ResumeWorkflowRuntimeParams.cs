using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime.Parameters;

/// <summary>
/// Options for resuming workflows.
/// </summary>
[Obsolete("This type is obsolete. Use the new CreateClientAsync methods of IWorkflowRuntime instead.")]
public class ResumeWorkflowRuntimeParams
{
    public string? CorrelationId { get; set; }
    public string? BookmarkId { get; set; }
    public ActivityHandle? ActivityHandle { get; set; }
    public IDictionary<string, object>? Input { get; set; }
    public IDictionary<string, object>? Properties { get; set; }
    public CancellationToken CancellationToken { get; set; }
}