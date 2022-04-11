using Elsa.Models;

namespace Elsa.Runtime.Models;

public record InvokeWorkflowInstanceRequest(string InstanceId, Bookmark? Bookmark = default, IDictionary<string, object>? Input = default, string? CorrelationId = default);