using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Results;

public record CancellationResult(bool Result, FailureReason? Reason = default);