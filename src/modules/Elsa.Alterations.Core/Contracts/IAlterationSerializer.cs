namespace Elsa.Alterations.Core.Contracts;

/// <summary>
/// Serializes and deserializes <see cref="IAlteration"/> objects.
/// </summary>
public interface IAlterationSerializer
{
    /// <summary>
    /// Serializes the specified <see cref="IAlteration"/> object.
    /// </summary>
    string Serialize(IAlteration alteration);

    /// <summary>
    /// Serializes the specified set of <see cref="IAlteration"/> objects.
    /// </summary>
    string SerializeMany(IEnumerable<IAlteration> alterations);
    
    /// <summary>
    /// Deserializes the specified JSON string into an <see cref="IAlteration"/> object.
    /// </summary>
    IAlteration Deserialize(string json);
    
    /// <summary>
    /// Deserializes the specified JSON string into a set of <see cref="IAlteration"/> objects.
    /// </summary>
    IEnumerable<IAlteration> DeserializeMany(string json);
}