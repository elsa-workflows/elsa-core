using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Runtime.Models;

/// <summary>
/// Represents a workflow definition and its workflow.
/// </summary>
/// <param name="Workflow">The workflow materialized from its workflow definition.</param>
/// <param name="MaterializerName">The name of the materializer that materialized the workflow.</param>
/// <param name="MaterializerContext">The context of the materializer that materialized the workflow.</param>
public record MaterializedWorkflow(Workflow Workflow, string MaterializerName, object? MaterializerContext = default);