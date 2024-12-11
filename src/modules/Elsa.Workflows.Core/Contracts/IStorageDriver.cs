namespace Elsa.Workflows;

/// <summary>
/// Represents a storage driver for workflows to write state to.
/// </summary>
public interface IStorageDriver
{
    /// <summary>
    /// The priority of the storage driver. Drivers with higher priority are used before drivers with lower priority.
    /// </summary>
    double Priority { get; }
    
    /// <summary>
    /// A list of tags assigned to the driver used to describe the data contents.
    /// </summary>
    IEnumerable<string> Tags { get; }
    
    /// <summary>
    /// Writes a value to the storage driver.
    /// </summary>
    /// <param name="id">The ID of the value to write.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="context">Context about the workflow execution.</param>
    ValueTask WriteAsync(string id, object value, StorageDriverContext context);
    /// <summary>
    /// Reads a value from the storage driver.
    /// </summary>
    /// <param name="id">The ID of the value to read.</param>
    /// <param name="context">Context about the workflow execution.</param>
    /// <returns>The value read from the storage driver.</returns>
    ValueTask<object?> ReadAsync(string id, StorageDriverContext context);
    
    /// <summary>
    /// Deletes a value from the storage driver.
    /// </summary>
    /// <param name="id">The ID of the value to delete.</param>
    /// <param name="context">Context about the workflow execution.</param>
    ValueTask DeleteAsync(string id, StorageDriverContext context);
}