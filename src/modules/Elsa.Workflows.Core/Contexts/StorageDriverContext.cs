using Elsa.Workflows.Contracts;

namespace Elsa.Workflows;

/// <summary>
/// Provides context for storage drivers.
/// </summary>
public record StorageDriverContext(IExecutionContext ExecutionContext, CancellationToken CancellationToken);