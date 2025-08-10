using Elsa.Workflows.Models;

namespace Elsa.Testing.Framework.Models;

public record AssertionContext(RunWorkflowResult RunWorkflowResult, CancellationToken CancellationToken);