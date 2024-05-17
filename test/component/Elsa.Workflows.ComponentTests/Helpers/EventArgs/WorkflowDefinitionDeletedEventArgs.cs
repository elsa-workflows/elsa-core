namespace Elsa.Workflows.ComponentTests;

public class WorkflowDefinitionDeletedEventArgs(string definitionId) : EventArgs 
{
    public string DefinitionId { get; } = definitionId;
}