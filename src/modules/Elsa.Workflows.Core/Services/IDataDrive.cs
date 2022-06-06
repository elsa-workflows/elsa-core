using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Services;

/// <summary>
/// Represents a storage driver for workflows to write state to.
/// </summary>
public interface IDataDrive
{
    string Id { get; }
    ValueTask WriteAsync(string id, object value, DataDriveContext context);
    ValueTask<object?> ReadAsync(string id, DataDriveContext context);
    ValueTask DeleteAsync(string id, DataDriveContext context);
}

public record DataDriveContext(WorkflowState WorkflowState, CancellationToken CancellationToken);