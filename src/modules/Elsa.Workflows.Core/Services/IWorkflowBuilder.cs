using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Services;

public interface IWorkflowBuilder
{
    string? DefinitionId { get; }
    int Version { get; }
    IActivity? Root { get; }
    ICollection<Variable> Variables { get; set; }
    IDictionary<string, object> ApplicationProperties { get; }
    IWorkflowBuilder WithDefinitionId(string definitionId);
    IWorkflowBuilder WithVersion(int version);
    IWorkflowBuilder WithRoot(IActivity root);
    Variable<T> WithVariable<T>(string? storageDriverId = default);
    Variable<T> WithVariable<T>(string name, T value, string? storageDriverId = default);
    Variable<T> WithVariable<T>(T value, string? storageDriverId = default);
    IWorkflowBuilder WithVariable(Variable variable);
    IWorkflowBuilder WithVariables(params Variable[] variables);
    IWorkflowBuilder WithApplicationProperty(string name, object value);
    Workflow BuildWorkflow();
    Task<Workflow> BuildWorkflowAsync(IWorkflow workflowDefinition, CancellationToken cancellationToken = default);
}