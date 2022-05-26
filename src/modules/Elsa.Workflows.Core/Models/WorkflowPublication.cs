namespace Elsa.Workflows.Core.Models;

public record WorkflowPublication(bool IsLatest, bool IsPublished)
{
    public static WorkflowPublication LatestAndPublished => new(true, true);
    public static WorkflowPublication LatestDraft => new(true, false);
    public static WorkflowPublication Draft => new(false, false);
}