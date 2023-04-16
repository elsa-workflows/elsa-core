namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Serializes and deserializes activities.
/// </summary>
public interface IActivitySerializer
{
    /// <summary>
    /// Serializes the specified activity.
    /// </summary>
    /// <param name="activity">The activity to serialize.</param>
    /// <returns>The serialized activity.</returns>
    string Serialize(IActivity activity);
    
    /// <summary>
    /// Deserializes the specified serialized activity.
    /// </summary>
    /// <param name="serializedActivity">The serialized activity.</param>
    /// <returns>The deserialized activity.</returns>
    IActivity Deserialize(string serializedActivity);
}