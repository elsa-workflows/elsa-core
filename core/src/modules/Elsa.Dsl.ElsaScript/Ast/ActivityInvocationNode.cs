namespace Elsa.Dsl.ElsaScript.Ast;

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
    public List<ArgumentNode> Arguments { get; set; } = [];

    /// <summary>
    /// Optional variable to capture the result.
    /// </summary>
    public string? ResultVariable { get; set; }
}