using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Services;

/// <summary>
/// Represents a storage driver for workflows to write state to.
/// </summary>
public interface IStorageDriver
{
    ValueTask WriteAsync(string id, object value, StorageDriverContext context);
    ValueTask<object?> ReadAsync(string id, StorageDriverContext context);
    ValueTask DeleteAsync(string id, StorageDriverContext context);
}

public record StorageDriverContext(WorkflowExecutionContext WorkflowExecutionContext, CancellationToken CancellationToken);