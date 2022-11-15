using System.Collections.Generic;

namespace Elsa.Workflows.Runtime.Models;

public record DispatchWorkflowInstanceRequest(string InstanceId, string BookmarkId, IDictionary<string, object>? Input = default, string? CorrelationId = default);