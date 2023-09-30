using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Elsa.Alterations.Core.Contracts;
using Elsa.Workflows.Core;

namespace Elsa.Alterations.Serialization;

/// <summary>
/// Add additional <see cref="JsonConverter"/> objects.
/// </summary>
public class AlterationSerializationOptionConfigurator : SerializationOptionsConfiguratorBase
{
    /// <inheritdoc />
    public override IEnumerable<Action<JsonTypeInfo>> GetModifiers()
    {
        yield return static typeInfo =>
        {
            if (typeInfo.Type != typeof(IAlteration))
                return;

            typeInfo.PolymorphismOptions = new()
            {
                TypeDiscriminatorPropertyName = "type",
                DerivedTypes =
                {
                    // TODO: Make this list extensible.
                    new JsonDerivedType(typeof(Migrate), nameof(Migrate)),
                    new JsonDerivedType(typeof(ModifyVariable), nameof(ModifyVariable)),
                    new JsonDerivedType(typeof(ScheduleActivity), nameof(ScheduleActivity))
                }
            };
        };
    }
}