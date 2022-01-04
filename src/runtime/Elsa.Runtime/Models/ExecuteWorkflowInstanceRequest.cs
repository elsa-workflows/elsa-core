using Elsa.Models;

namespace Elsa.Runtime.Models;

public record ExecuteWorkflowInstanceRequest(string InstanceId, Bookmark? Bookmark = default);