using Elsa.Expressions.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

namespace Elsa.Workflows;

/// <summary>
/// Extension methods for <see cref="IActivityBuilder"/> to add common activities.
/// </summary>
public static class ActivityBuilderExtensions
{
    /// <summary>
    /// Sets a variable to the specified value using an expression.
    /// </summary>
    /// <param name="builder">The activity builder.</param>
    /// <param name="variableName">The name of the variable to set.</param>
    /// <param name="expression">The expression to evaluate for the value.</param>
    /// <returns>The activity builder for chaining.</returns>
    public static IActivityBuilder SetVar(this IActivityBuilder builder, string variableName, string expression)
    {
        // Find the variable in the workflow
        var variable = builder.WorkflowBuilder.Variables.FirstOrDefault(v => v.Name == variableName);
        
        if (variable == null)
        {
            // Create a new variable if it doesn't exist
            variable = new Variable<object>(variableName, new object());
            builder.WorkflowBuilder.Variables.Add(variable);
        }
        
        var setVariable = new SetVariable
        {
            Variable = variable,
            Value = new Input<object?>(new Expression("Liquid", expression))
        };
        
        return builder.Then(setVariable);
    }
    
    /// <summary>
    /// Sets a variable to a literal value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="builder">The activity builder.</param>
    /// <param name="variableName">The name of the variable to set.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>The activity builder for chaining.</returns>
    public static IActivityBuilder SetVar<T>(this IActivityBuilder builder, string variableName, T value)
    {
        var variable = builder.WorkflowBuilder.Variables.FirstOrDefault(v => v.Name == variableName) as Variable<T>;
        
        if (variable == null)
        {
            variable = new Variable<T>(variableName, default!);
            builder.WorkflowBuilder.Variables.Add(variable);
        }
        
        var setVariable = new SetVariable<T>(variable, value);
        
        return builder.Then(setVariable);
    }
    
    /// <summary>
    /// Writes a line of text to the console.
    /// </summary>
    /// <param name="builder">The activity builder.</param>
    /// <param name="text">The text to write.</param>
    /// <returns>The activity builder for chaining.</returns>
    public static IActivityBuilder Log(this IActivityBuilder builder, string text)
    {
        return builder.Then(new WriteLine(text));
    }
    
    /// <summary>
    /// Sets a name for the previous activity in the chain.
    /// </summary>
    /// <param name="builder">The activity builder.</param>
    /// <param name="name">The name to set.</param>
    /// <returns>The activity builder for chaining.</returns>
    public static IActivityBuilder Named(this IActivityBuilder builder, string name)
    {
        if (builder.Activity is Activity activity)
        {
            activity.Name = name;
        }
        return builder;
    }
}
