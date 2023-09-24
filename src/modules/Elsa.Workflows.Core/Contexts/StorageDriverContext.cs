using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core;

/// <summary>
/// Provides context for storage drivers.
/// </summary>
public record StorageDriverContext(IExecutionContext ExecutionContext, CancellationToken CancellationToken);