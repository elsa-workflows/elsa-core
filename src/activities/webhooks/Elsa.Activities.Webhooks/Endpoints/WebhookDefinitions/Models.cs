using System.ComponentModel.DataAnnotations;

namespace Elsa.Activities.Webhooks.Endpoints.WebhookDefinitions
{
    public sealed record SaveWebhookDefinitionRequest
    {
        public string? Id { get; init; }
        [Required] public string Path { get; init; } = default!;
        [Required] public string Name { get; init; }= default!;
        public string? Description { get; init; }
        public string? PayloadTypeName { get; init; }
        public bool IsEnabled { get; init; }
    }

    public record WebhookDefinitionSummaryModel(
        string Id,
        string? Path,
        string? Name,
        string? Description,
        string? PayloadTypeName,
        bool IsEnabled);
}