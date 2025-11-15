namespace Elsa.Dsl.ElsaScript.Ast;

/// <summary>
/// Base class for statement nodes.
/// </summary>
public abstract class StatementNode : AstNode
{
}

/// <summary>
/// Represents a variable declaration.
/// </summary>
public class VariableDeclarationNode : StatementNode
{
    /// <summary>
    /// The variable kind (var, let, const).
    /// </summary>
    public VariableKind Kind { get; set; }

    /// <summary>
    /// The variable name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The initial value expression.
    /// </summary>
    public ExpressionNode? Value { get; set; }
}

/// <summary>
/// The kind of variable declaration.
/// </summary>
public enum VariableKind
{
    Var,
    Let,
    Const
}

/// <summary>
/// Represents an activity invocation.
/// </summary>
public class ActivityInvocationNode : StatementNode
{
    /// <summary>
    /// The activity name (e.g., "WriteLine", "SendEmail").
    /// </summary>
    public string ActivityName { get; set; } = string.Empty;

    /// <summary>
    /// The arguments passed to the activity.
    /// </summary>
    public List<ArgumentNode> Arguments { get; set; } = new();

    /// <summary>
    /// Optional variable to capture the result.
    /// </summary>
    public string? ResultVariable { get; set; }
}

/// <summary>
/// Represents a block of statements (maps to Sequence).
/// </summary>
public class BlockNode : StatementNode
{
    /// <summary>
    /// The statements in the block.
    /// </summary>
    public List<StatementNode> Statements { get; set; } = new();
}

/// <summary>
/// Represents an if statement.
/// </summary>
public class IfNode : StatementNode
{
    /// <summary>
    /// The condition expression.
    /// </summary>
    public ExpressionNode Condition { get; set; } = null!;

    /// <summary>
    /// The then branch.
    /// </summary>
    public StatementNode Then { get; set; } = null!;

    /// <summary>
    /// The optional else branch.
    /// </summary>
    public StatementNode? Else { get; set; }
}

/// <summary>
/// Represents a foreach statement.
/// </summary>
public class ForEachNode : StatementNode
{
    /// <summary>
    /// The iterator variable name.
    /// </summary>
    public string VariableName { get; set; } = string.Empty;

    /// <summary>
    /// The collection expression.
    /// </summary>
    public ExpressionNode Collection { get; set; } = null!;

    /// <summary>
    /// The body statement.
    /// </summary>
    public StatementNode Body { get; set; } = null!;
}

/// <summary>
/// Represents a for statement.
/// </summary>
public class ForNode : StatementNode
{
    /// <summary>
    /// The initializer statement.
    /// </summary>
    public StatementNode? Initializer { get; set; }

    /// <summary>
    /// The condition expression.
    /// </summary>
    public ExpressionNode? Condition { get; set; }

    /// <summary>
    /// The iterator expression.
    /// </summary>
    public ExpressionNode? Iterator { get; set; }

    /// <summary>
    /// The body statement.
    /// </summary>
    public StatementNode Body { get; set; } = null!;
}

/// <summary>
/// Represents a while statement.
/// </summary>
public class WhileNode : StatementNode
{
    /// <summary>
    /// The condition expression.
    /// </summary>
    public ExpressionNode Condition { get; set; } = null!;

    /// <summary>
    /// The body statement.
    /// </summary>
    public StatementNode Body { get; set; } = null!;
}

/// <summary>
/// Represents a switch statement.
/// </summary>
public class SwitchNode : StatementNode
{
    /// <summary>
    /// The switch expression.
    /// </summary>
    public ExpressionNode Expression { get; set; } = null!;

    /// <summary>
    /// The cases.
    /// </summary>
    public List<SwitchCaseNode> Cases { get; set; } = new();

    /// <summary>
    /// The optional default case.
    /// </summary>
    public StatementNode? Default { get; set; }
}

/// <summary>
/// Represents a switch case.
/// </summary>
public class SwitchCaseNode : AstNode
{
    /// <summary>
    /// The case value.
    /// </summary>
    public ExpressionNode Value { get; set; } = null!;

    /// <summary>
    /// The case body.
    /// </summary>
    public StatementNode Body { get; set; } = null!;
}

/// <summary>
/// Represents a flowchart definition.
/// </summary>
public class FlowchartNode : StatementNode
{
    /// <summary>
    /// The labeled activities in the flowchart.
    /// </summary>
    public List<LabeledActivityNode> Activities { get; set; } = new();

    /// <summary>
    /// The connections between activities.
    /// </summary>
    public List<ConnectionNode> Connections { get; set; } = new();

    /// <summary>
    /// The optional entry point label.
    /// </summary>
    public string? EntryPoint { get; set; }
}

/// <summary>
/// Represents a labeled activity in a flowchart.
/// </summary>
public class LabeledActivityNode : AstNode
{
    /// <summary>
    /// The label.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// The activity invocation.
    /// </summary>
    public ActivityInvocationNode Activity { get; set; } = null!;
}

/// <summary>
/// Represents a connection in a flowchart.
/// </summary>
public class ConnectionNode : AstNode
{
    /// <summary>
    /// The source label.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// The optional outcome.
    /// </summary>
    public string? Outcome { get; set; }

    /// <summary>
    /// The target label.
    /// </summary>
    public string Target { get; set; } = string.Empty;
}

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
