using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Serialization.VariableExpressions;

/// <inheritdoc />
public class NumberActivity : CodeActivity
{
    /// <inheritdoc />
    [JsonConstructor]
    public NumberActivity()
    {
    }
    
    /// <inheritdoc />
    public NumberActivity(Variable<int> variable)
    {
        Number = new(variable);
    }
    
    /// <inheritdoc />
    public NumberActivity(Literal<int> literal)
    {
        Number = new(literal);
    }

    /// <summary>
    /// Gets or sets the number.
    /// </summary>
    public Input<int> Number { get; set; } = default!;

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        var number = Number.Get(context);
        Console.WriteLine(number.ToString());
    }
}