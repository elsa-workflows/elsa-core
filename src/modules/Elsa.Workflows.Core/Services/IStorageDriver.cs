using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Services;

/// <summary>
/// Represents a storage driver for workflows to write state to.
/// </summary>
public interface IStorageDriver
{
    string Id { get; }
    string DisplayName { get; }
    ValueTask WriteAsync(string id, object value, DataDriveContext context);
    ValueTask<object?> ReadAsync(string id, DataDriveContext context);
    ValueTask DeleteAsync(string id, DataDriveContext context);
}

public record DataDriveContext(WorkflowExecutionContext WorkflowExecutionContext, CancellationToken CancellationToken);