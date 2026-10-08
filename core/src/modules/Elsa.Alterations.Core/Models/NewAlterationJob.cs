namespace Elsa.Alterations.Core.Models;

public class NewAlterationJob
{
    public string PlanId { get; set; } = null!;
    public string WorkflowInstanceId { get; set; } = null!;
}