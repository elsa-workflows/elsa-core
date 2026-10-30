using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// A workflow of an <see cref="ITenantAgnosticWorkflowsProvider"/> together with the values needed to tell whether a
/// stored <see cref="WorkflowDefinition"/> already holds it.
/// </summary>
/// <param name="MaterializedWorkflow">The materialized workflow.</param>
/// <param name="IsTenantAgnostic">Whether the workflow belongs to every tenant.</param>
/// <param name="IsLatest">Whether the stored definition of the workflow is marked as the latest version once all imported workflows are stored.</param>
/// <param name="IsPublished">Whether the stored definition of the workflow is marked as published once all imported workflows are stored.</param>
/// <param name="StringData">The serialized root activity, if the workflow stores one.</param>
/// <param name="MaterializerContext">The serialized materializer context, if any.</param>
/// <param name="Content">The serialized variables, inputs, outputs, outcomes, custom properties and options, or <c>null</c> when the workflow could not be serialized and therefore cannot be compared.</param>
internal record ImportedWorkflow(MaterializedWorkflow MaterializedWorkflow, bool IsTenantAgnostic, bool IsLatest, bool IsPublished, string? StringData, string? MaterializerContext, string? Content);
