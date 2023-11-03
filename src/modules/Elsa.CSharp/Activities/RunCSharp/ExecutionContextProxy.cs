// ReSharper disable once CheckNamespace
namespace Elsa.CSharp.Models;

public partial class Globals
{
    /// <summary>
    /// Gets the key of the outcomes property.
    /// </summary>
    public static readonly object OutcomePropertiesKey = new();
    
    /// <summary>
    /// Sets the outcome of the current activity.
    /// </summary>
    /// <param name="outcomeName">The name of the outcome.</param>
    public void SetOutcome(string outcomeName)
    {
        ExpressionExecutionContext.TransientProperties[OutcomePropertiesKey] = new[] { outcomeName };
    }
    
    /// <summary>
    /// Sets the outcome of the current activity.
    /// </summary>
    /// <param name="outcomeNames">The names of the outcomes.</param>
    public void SetOutcomes(params string[] outcomeNames)
    {
        ExpressionExecutionContext.TransientProperties[OutcomePropertiesKey] = outcomeNames;
    }
}