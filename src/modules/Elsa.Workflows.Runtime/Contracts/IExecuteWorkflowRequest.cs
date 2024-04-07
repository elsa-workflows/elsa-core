using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime.Contracts;

public interface IExecuteWorkflowRequest
{
    string DefinitionVersionId { get; set; }
    string? CorrelationId { get; set; }
    string? BookmarkId { get; set; }
    ActivityHandle? ActivityHandle { get; set; }
    IDictionary<string, object>? Input { get; set; }
    IDictionary<string, object>? Properties { get; set; }
    string? TriggerActivityId { get; set; }
    string? ParentWorkflowInstanceId { get; set; }
}