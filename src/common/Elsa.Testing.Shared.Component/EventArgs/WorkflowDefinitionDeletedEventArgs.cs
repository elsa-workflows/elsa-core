namespace Elsa.Testing.Shared.EventArgs;

public class WorkflowDefinitionDeletedEventArgs(string definitionId) : System.EventArgs 
{
    public string DefinitionId { get; } = definitionId;
}