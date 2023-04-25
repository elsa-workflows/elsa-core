namespace Elsa.Samples.Onboarding.Web.Models;

public record RunTaskWebhook(
    string WorkflowInstanceId,
    string TaskId, 
    string TaskName, 
    TaskPayload TaskPayload);