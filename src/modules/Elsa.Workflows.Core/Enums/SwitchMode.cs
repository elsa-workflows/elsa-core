namespace Elsa.Workflows.Core;

/// <summary>
/// Represents the switch mode.
/// </summary>
public enum SwitchMode
{
    /// <summary>
    /// Yields the outcome of the first condition evaluating to true.
    /// </summary>
    MatchFirst,

    /// <summary>
    /// Yields the outcome of all conditions evaluating to true.
    /// </summary>
    MatchAny
}