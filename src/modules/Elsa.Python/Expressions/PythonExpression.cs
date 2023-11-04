using Elsa.Expressions.Contracts;

namespace Elsa.Python.Expressions;

/// <summary>
/// A Python expression.
/// </summary>
public class PythonExpression : IExpression
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PythonExpression"/> class.
    /// </summary>
    /// <param name="value">The Python expression.</param>
    public PythonExpression(string value)
    {
        Value = value;
    }
        
    /// <summary>
    /// Gets the Python expression.
    /// </summary>
    public string Value { get; }
}