using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management;

/// <summary>
/// Reports diagnostics from an existing workflow graph without evaluating its expressions or executing activities.
/// </summary>
public interface IWorkflowDiagnosticProvider
{
    /// <summary>Inspects the supplied graph and returns the diagnostics it can establish statically.</summary>
    IEnumerable<WorkflowValidationError> GetDiagnostics(WorkflowGraph graph);
}
