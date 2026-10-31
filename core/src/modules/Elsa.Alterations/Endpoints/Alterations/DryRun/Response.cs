namespace Elsa.Alterations.Endpoints.Alterations.DryRun;

/// <summary>
/// The response from the <see cref="DryRun"/> endpoint.
/// </summary>
public record Response(ICollection<string> WorkflowInstanceIds);