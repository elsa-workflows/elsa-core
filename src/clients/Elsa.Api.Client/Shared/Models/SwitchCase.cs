using System.Diagnostics;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Expressions;

namespace Elsa.Api.Client.Shared.Models;

/// <summary>
/// Represents a case in a switch statement.
/// </summary>
public class SwitchCase
{
    /// <summary>
    /// Gets or sets the label of the case.
    /// </summary>
    public string Label { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the condition of the case.
    /// </summary>
    public IExpression Condition { get; set; } = new JavaScriptExpression("");

    /// <summary>
    /// When used in a <see cref="Switch"/> activity, specifies the activity to schedule when the condition evaluates to true.
    /// </summary>
    public JsonObject? Activity { get; set; }
}