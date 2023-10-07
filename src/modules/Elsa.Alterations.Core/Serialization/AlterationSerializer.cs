using System.Text.Json;
using Elsa.Alterations.Core.Contracts;
using Elsa.Common.Serialization;

namespace Elsa.Alterations.Core.Serialization;

/// <summary>
/// A serializer for <see cref="IAlteration"/> objects.
/// </summary>
public class AlterationSerializer : ConfigurableSerializer, IAlterationSerializer
{
    /// <inheritdoc />
    public AlterationSerializer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    /// <inheritdoc />
    public string Serialize(IAlteration alteration)
    {
        var options = CreateOptions();
        return JsonSerializer.Serialize(alteration, options);
    }

    /// <inheritdoc />
    public string SerializeMany(IEnumerable<IAlteration> alterations)
    {
        var options = CreateOptions();
        return JsonSerializer.Serialize(alterations.ToArray(), options);
    }

    /// <inheritdoc />
    public IAlteration Deserialize(string json)
    {
        var options = CreateOptions();
        return JsonSerializer.Deserialize<IAlteration>(json, options)!;
    }

    /// <inheritdoc />
    public IEnumerable<IAlteration> DeserializeMany(string json)
    {
        var options = CreateOptions();
        return JsonSerializer.Deserialize<IAlteration[]>(json, options)!;
    }
}