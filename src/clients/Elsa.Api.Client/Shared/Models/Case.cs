using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Expressions;

namespace Elsa.Api.Client.Shared.Models;

/// <summary>
/// Represents a case in a switch expression.
/// </summary>
public class Case
{
    /// <summary>
    /// Gets or sets the label of the case.
    /// </summary>
    public string Label { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the condition of the case.
    /// </summary>
    public IExpression Condition { get; set; } = new JavaScriptExpression("");
}