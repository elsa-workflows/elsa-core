namespace Elsa.Workflows.Core.Activities.Flowchart.Attributes;

/// <summary>
/// Defines the outcomes of a flow node.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class FlowNodeAttribute : Attribute
{
    /// <inheritdoc />
    public FlowNodeAttribute(params string[] outcomes)
    {
        Outcomes = outcomes;
    }
    
    /// <summary>
    /// The outcomes of the flow node.
    /// </summary>
    public ICollection<string> Outcomes { get; }
}