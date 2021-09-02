namespace Elsa.Webhooks.Api.Models
{
    public record WebhookDefinitionSummaryModel(
        string Id,
        string? Path,
        string? Name,
        string? Description,
        string? PayloadTypeName,
        bool IsEnabled);
}
