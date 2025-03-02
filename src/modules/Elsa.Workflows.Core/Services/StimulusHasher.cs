namespace Elsa.Workflows;

/// <inheritdoc />
public class StimulusHasher(IHasher hasher) : IStimulusHasher
{
    /// <inheritdoc />
    public string Hash(string activityTypeName, object? payload = null, string? activityInstanceId = null)
    {
        return hasher.Hash(activityTypeName, payload, activityInstanceId);
    }
}