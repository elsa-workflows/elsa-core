namespace Trimble.Elsa.Activities.Activities.Expressions;

/// <summary>
/// Defines the variable to be matched in a regex pattern 
/// for use in the <see cref="RegexVariableExpressionHandler"/>
/// </summary>
public record VariableMatchCondition(string variableName, string matchPattern);