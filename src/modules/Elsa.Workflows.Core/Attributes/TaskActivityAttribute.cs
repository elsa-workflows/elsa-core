namespace Elsa.Workflows.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class TaskActivityAttribute : ActivityAttribute
{
    public TaskActivityAttribute()
    {
        Kind = ActivityKind.Task;
    }
    
    public TaskActivityAttribute(string @namespace, string? category, string? description = null, bool runAsynchronously = false) 
        : base(@namespace, category, description)
    {
        Kind = ActivityKind.Task;
        RunAsynchronously = runAsynchronously;
    }
    
    public bool RunAsynchronously { get; set; }
}