namespace Elsa.Activities.Webhooks.Endpoints.WebhookDefinitions
{
    public sealed record SaveRequest
    {
        public string? Id { get; init; }
        public string? Path { get; init; }
        public string? Name { get; init; }
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
