namespace Elsa.Studio.Workflows.Domain.Models.Bpmn;

/// <summary>
/// The <see cref="BpmnImportIssueModel.Severity"/> values the <c>bpmn/analyze</c> and <c>bpmn/import</c> endpoints
/// send. Not an enum because the wire type is a plain string; these constants exist so callers match against a
/// shared name instead of repeating the literals.
/// </summary>
public static class BpmnImportIssueSeverity
{
    /// <summary>The element's authored meaning could not be represented and will not run.</summary>
    public const string Dropped = "Dropped";

    /// <summary>The element ran, but not exactly as authored.</summary>
    public const string Degraded = "Degraded";

    /// <summary>The element was imported without loss; reported for visibility only.</summary>
    public const string Info = "Info";
}
