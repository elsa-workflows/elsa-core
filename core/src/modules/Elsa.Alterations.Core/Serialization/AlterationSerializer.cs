using System.Diagnostics.CodeAnalysis;
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
    [RequiresUnreferencedCode("The type of the alteration must be known at compile time.")]
    public string Serialize(IAlteration alteration)
    {
        var options = GetOptions();
        return JsonSerializer.Serialize(alteration, options);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type of the alteration must be known at compile time.")]
    public string SerializeMany(IEnumerable<IAlteration> alterations)
    {
        var options = GetOptions();
        return JsonSerializer.Serialize(alterations.ToArray(), options);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type of the alteration must be known at compile time.")]
    public IAlteration Deserialize(string json)
    {
        var options = GetOptions();
        return JsonSerializer.Deserialize<IAlteration>(json, options)!;
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type of the alteration must be known at compile time.")]
    public IEnumerable<IAlteration> DeserializeMany(string json)
    {
        var options = GetOptions();
        return JsonSerializer.Deserialize<IAlteration[]>(json, options)!;
    }
}