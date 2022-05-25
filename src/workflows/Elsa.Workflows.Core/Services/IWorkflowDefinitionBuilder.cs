using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Services;

public interface IWorkflowDefinitionBuilder
{
    string? DefinitionId { get; }
    int Version { get; }
    IActivity? Root { get; }
    ICollection<Variable> Variables { get; set; }
    IDictionary<string, object> ApplicationProperties { get; }
    IWorkflowDefinitionBuilder WithDefinitionId(string definitionId);
    IWorkflowDefinitionBuilder WithVersion(int version);
    IWorkflowDefinitionBuilder WithRoot(IActivity root);
    IWorkflowDefinitionBuilder WithVariable(Variable variable);
    IWorkflowDefinitionBuilder WithVariables(params Variable[] variables);
    IWorkflowDefinitionBuilder WithApplicationProperty(string name, object value);
    Workflow BuildWorkflow();
    Task<Workflow> BuildWorkflowAsync(IWorkflow workflowDefinition, CancellationToken cancellationToken = default);
}