using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Runtime.Models;

public record DispatchWorkflowInstanceRequest(string InstanceId, Bookmark? Bookmark = default, IDictionary<string, object>? Input = default, string? CorrelationId = default);