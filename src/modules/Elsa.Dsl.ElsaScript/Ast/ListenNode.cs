namespace Elsa.Dsl.ElsaScript.Ast;

/// <summary>
/// Represents a listen statement (for triggers).
/// </summary>
public class ListenNode : StatementNode
{
    /// <summary>
    /// The activity invocation for the trigger.
    /// </summary>
    public ActivityInvocationNode Activity { get; set; } = null!;
}