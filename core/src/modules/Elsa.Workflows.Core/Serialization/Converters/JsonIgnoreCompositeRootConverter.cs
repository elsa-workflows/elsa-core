using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Serialization.Helpers;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// Ignores properties with the <see cref="JsonIgnoreCompositeRootAttribute"/> attribute.
/// </summary>
public class JsonIgnoreCompositeRootConverter(ActivityWriter activityWriter) : JsonConverter<IActivity>
{
    /// <inheritdoc />
    public override IActivity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public override void Write(Utf8JsonWriter writer, IActivity? value, JsonSerializerOptions options)
    {
        activityWriter.WriteActivity(writer, value, options, ignoreSpecializedConverters: true, propertyFilter: property => property.GetCustomAttribute<JsonIgnoreCompositeRootAttribute>() != null);
    }
}