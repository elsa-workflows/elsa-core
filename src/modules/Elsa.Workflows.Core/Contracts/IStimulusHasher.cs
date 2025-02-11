namespace Elsa.Workflows;

/// <summary>
/// Computes a hash for a given activity type name and bookmark payload.
/// </summary>
public interface IStimulusHasher
{
    /// <summary>
    /// Produces a hash from the specified activity type name, payload and activity instance ID.
    /// </summary>
    string Hash(string activityTypeName, object? payload = null, string? activityInstanceId = null);
}