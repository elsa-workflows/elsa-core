namespace Elsa.Workflows;

/// <inheritdoc />
public class StimulusHasher(IHasher hasher) : IStimulusHasher
{
    /// <inheritdoc />
    public string Hash(string stimulusName, object? payload = null, string? activityInstanceId = null)
    {
        return hasher.Hash(stimulusName, payload, activityInstanceId);
    }
}