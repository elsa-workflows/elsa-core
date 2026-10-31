using System.Text.Json;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Shared.UIHints.RadioList;

namespace Elsa.Studio.UIHints.Extensions;

/// <summary>
/// Provides extension methods for <see cref="InputDescriptor"/>.
/// </summary>
public static class InputDescriptorRadioListExtensions
{
    /// <summary>
    /// Gets a list of <see cref="RadioListItem"/>s for the specified <see cref="InputDescriptor"/>.
    /// </summary>
    public static RadioList GetRadioList(this InputDescriptor descriptor)
    {
        var specifications = descriptor.UISpecifications;
        var props = specifications != null ? specifications.TryGetValue(InputUIHints.RadioList, out var propsValue) ? propsValue is JsonElement value ? value : default : default : default;

        if (props.ValueKind == JsonValueKind.Undefined)
            return new RadioList(Array.Empty<RadioListItem>());

        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        var radioListProps = props.Deserialize<RadioListProps>(serializerOptions);
        return radioListProps?.RadioList ?? new RadioList(Array.Empty<RadioListItem>());
    }
}