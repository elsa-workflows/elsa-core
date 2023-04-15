using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Workflows.Core.Serialization.ReferenceHandlers;

/// <summary>
/// A custom reference resolver that uses a <see cref="ReferenceEqualityComparer"/> to compare objects.
/// </summary>
/// <remarks>We only need this class because the built-in <see cref="PreserveReferenceResolver"/> is internal</remarks>
public class CustomPreserveReferenceResolver : ReferenceResolver
{
    private uint _referenceCount;
    private readonly Dictionary<string, object> _referenceIdToObjectMap = new();
    private readonly Dictionary<object, string> _objectToReferenceIdMap = new(ReferenceEqualityComparer.Instance);

    /// <inheritdoc />
    public override void AddReference(string referenceId, object value)
    {
        if (!_referenceIdToObjectMap.TryAdd(referenceId, value))
            throw new JsonException();
    }

    /// <inheritdoc />
    public override string GetReference(object value, out bool alreadyExists)
    {
        if (_objectToReferenceIdMap.TryGetValue(value, out var referenceId))
        {
            alreadyExists = true;
        }
        else
        {
            _referenceCount++;
            referenceId = _referenceCount.ToString();
            _objectToReferenceIdMap.Add(value, referenceId);
            alreadyExists = false;
        }

        return referenceId;
    }

    /// <inheritdoc />
    public override object ResolveReference(string referenceId) =>
        !_referenceIdToObjectMap.TryGetValue(referenceId, out var value) ? throw new JsonException() : value;
}