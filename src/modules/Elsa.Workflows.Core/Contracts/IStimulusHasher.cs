namespace Elsa.Workflows;

/// Computes a hash for a given activity type name and bookmark payload.
public interface IStimulusHasher
{
    /// Produces a hash from the specified activity type name, payload and activity instance ID.
    string Hash(string activityTypeName, object? payload = null, string? activityInstanceId = null);
}