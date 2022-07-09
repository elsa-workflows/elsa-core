using System.Threading;
using Elsa.Models;

namespace Elsa.Retention.Models;

public record RetentionFilterContext(WorkflowInstance WorkflowInstance, CancellationToken CancellationToken);