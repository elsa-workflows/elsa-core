using Elsa.Models;

namespace Elsa.Runtime.Models;

public record DispatchWorkflowInstanceRequest(string InstanceId, Bookmark? Bookmark = default);