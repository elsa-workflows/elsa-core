namespace Elsa.Scripting.ElsaScript.Ast;

/// <summary>
/// Represents a state machine container.
/// </summary>
public class StateMachine : Statement
{
    public List<State> States { get; set; } = new();
    public string Start { get; set; }
}

/// <summary>
/// A state in the state machine.
/// </summary>
public class State : AstNode
{
    public string Name { get; set; }
    public List<Transition> Transitions { get; set; } = new();
    public Block Entry { get; set; }
    public Block Exit { get; set; }
}

/// <summary>
/// A transition in a state.
/// </summary>
public class Transition : AstNode
{
    public string Event { get; set; }
    public string TargetState { get; set; }
}
