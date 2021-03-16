namespace Elsa.Activities.Telnyx.Client.Models
{
    public record AnswerCallRequest(string? BillingGroupId, string? ClientState, string? CommandId, string? WebhookUrl, string? WebhookUrlMethod);
}