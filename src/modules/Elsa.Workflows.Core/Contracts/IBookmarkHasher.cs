namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Computes a hash for a given activity type name and bookmark payload.
/// </summary>
public interface IBookmarkHasher
{
    /// <summary>
    /// Produces a hash from the specified activity type name and bookmark payload.
    /// </summary>
    string Hash(string activityTypeName, object? payload);
}