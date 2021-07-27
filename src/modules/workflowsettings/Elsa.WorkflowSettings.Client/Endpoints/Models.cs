using System.ComponentModel.DataAnnotations;

namespace Elsa.WorkflowSettings.Client.Endpoints
{
    public sealed record SaveWorkflowSettingsRequest
    {
        public string? Id { get; init; }
        [Required] public string WorkflowBlueprintId { get; init; } = default!;
        [Required] public string Key { get; init; } = default!;
        [Required] public string Value { get; init; } = default!;
    }
}