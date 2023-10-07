using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Options;
using Elsa.Workflows.Core;
using Microsoft.Extensions.Options;

namespace Elsa.Alterations.Core.Serialization;

/// <summary>
/// Add additional <see cref="JsonConverter"/> objects.
/// </summary>
public class AlterationSerializationOptionConfigurator : SerializationOptionsConfiguratorBase
{
    private readonly IOptions<AlterationOptions> _options;

    /// <inheritdoc />
    public AlterationSerializationOptionConfigurator(IOptions<AlterationOptions> options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public override IEnumerable<Action<JsonTypeInfo>> GetModifiers()
    {
        var alterationTypes = _options.Value.AlterationTypes;

        yield return typeInfo =>
        {
            if (typeInfo.Type != typeof(IAlteration))
                return;

            var polymorphismOptions = new JsonPolymorphismOptions
            {
                TypeDiscriminatorPropertyName = "type"
            };

            foreach (var alterationType in alterationTypes.ToList())
            {
                polymorphismOptions.DerivedTypes.Add(new JsonDerivedType(alterationType, alterationType.Name));
            }

            typeInfo.PolymorphismOptions = polymorphismOptions;
        };
    }
}