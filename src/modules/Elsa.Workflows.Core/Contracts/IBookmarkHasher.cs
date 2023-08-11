namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Computes a hash for a given activity type name and bookmark payload.
/// </summary>
public interface IBookmarkHasher
{
    /// <summary>
    /// Produces a hash from the specified activity type name, bookmark payload and activity instance ID.
    /// </summary>
    string Hash(string activityTypeName, object? payload, string? activityInstanceId = default);
}