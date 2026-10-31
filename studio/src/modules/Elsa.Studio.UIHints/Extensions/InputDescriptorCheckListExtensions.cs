using System.Text.Json;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Shared.UIHints.CheckList;

namespace Elsa.Studio.UIHints.Extensions;

/// <summary>
/// Provides extension methods for <see cref="InputDescriptor"/>.
/// </summary>
public static class InputDescriptorCheckListExtensions
{
    /// <summary>
    /// Gets a list of <see cref="CheckListItem"/>s for the specified <see cref="InputDescriptor"/>.
    /// </summary>
    public static CheckList GetCheckList(this InputDescriptor descriptor)
    {
        var specifications = descriptor.UISpecifications;
        var props = specifications != null ? specifications.TryGetValue(InputUIHints.CheckList, out var propsValue) ? propsValue is JsonElement value ? value : default : default : default;

        if (props.ValueKind == JsonValueKind.Undefined)
            return new([]);

        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        var checkListProps = props.Deserialize<CheckListProps>(serializerOptions);
        return checkListProps?.CheckList ?? new CheckList([]);
    }
}