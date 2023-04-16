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
    /// Serializes the specified value.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The serialized value.</returns>
    string Serialize(object value);
    
    /// <summary>
    /// Deserializes the specified serialized activity.
    /// </summary>
    /// <param name="serializedActivity">The serialized activity.</param>
    /// <returns>The deserialized activity.</returns>
    IActivity Deserialize(string serializedActivity);
    
    /// <summary>
    /// Deserializes the specified serialized value.
    /// </summary>
    /// <param name="serializedValue">The serialized value.</param>
    /// <param name="type">The type of the value to deserialize.</param>
    /// <returns>The deserialized value.</returns>
    object Deserialize(string serializedValue, Type type);
    
    /// <summary>
    /// Deserializes the specified serialized value.
    /// </summary>
    /// <param name="serializedValue">The serialized value.</param>
    /// <returns>The deserialized value.</returns>
    T Deserialize<T>(string serializedValue);
}