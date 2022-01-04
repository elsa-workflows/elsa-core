using Elsa.Models;

namespace Elsa.Contracts;

public interface IWorkflowDefinitionBuilder
{
    string? DefinitionId { get; }
    int Version { get; }
    IActivity? Root { get; }
    ICollection<ITrigger> Triggers { get; }
    IWorkflowDefinitionBuilder WithDefinitionId(string definitionId);
    IWorkflowDefinitionBuilder WithVersion(int version);
    IWorkflowDefinitionBuilder WithRoot(IActivity root);
    IWorkflowDefinitionBuilder AddTrigger(ITrigger trigger);
    Workflow BuildWorkflow();
}