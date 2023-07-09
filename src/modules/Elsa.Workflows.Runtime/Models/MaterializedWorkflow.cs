using Elsa.Workflows.Core.Activities;

namespace Elsa.Workflows.Runtime.Models;

/// <summary>
/// Represents a workflow definition and its workflow.
/// </summary>
/// <param name="Workflow">The workflow materialized from its workflow definition.</param>
/// <param name="ProviderName">The name of the provider that provided the workflow definition.</param>
/// <param name="MaterializerName">The name of the materializer that materialized the workflow.</param>
/// <param name="MaterializerContext">The context of the materializer that materialized the workflow.</param>
public record MaterializedWorkflow(Workflow Workflow, string ProviderName, string MaterializerName, object? MaterializerContext = default);