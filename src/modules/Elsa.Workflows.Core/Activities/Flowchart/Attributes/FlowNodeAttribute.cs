namespace Elsa.Workflows.Core.Activities.Flowchart.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class FlowNodeAttribute : Attribute
{
    public FlowNodeAttribute(params string[] outcomes)
    {
        Outcomes = outcomes;
    }
    
    public ICollection<string> Outcomes { get; }
}