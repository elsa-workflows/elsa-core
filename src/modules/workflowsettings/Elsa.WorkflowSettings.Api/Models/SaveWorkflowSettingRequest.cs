using System.ComponentModel.DataAnnotations;

namespace Elsa.WorkflowSettings.Api.Models
{
    public sealed record SaveWorkflowSettingRequest
    {
        public string? Id { get; init; }
        [Required] public string WorkflowBlueprintId { get; init; } = default!;
        [Required] public string Key { get; init; } = default!;
        [Required] public string Value { get; init; } = default!;
    }
}