using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Domain.Contracts;

/// <summary>
/// Provides workflow root activity templates for new workflow definitions.
/// </summary>
public interface IWorkflowRootActivityTemplateProvider
{
    /// <summary>
    /// Returns all available workflow root activity templates.
    /// </summary>
    IReadOnlyCollection<WorkflowRootActivityTemplate> List();

    /// <summary>
    /// Returns the default workflow root activity template.
    /// </summary>
    WorkflowRootActivityTemplate GetDefault();

    /// <summary>
    /// Finds a workflow root activity template by key.
    /// </summary>
    WorkflowRootActivityTemplate? Find(string? key);
}
