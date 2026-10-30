using System.Reflection;
using Elsa.Workflows.UIHints.Dropdown;

namespace Elsa.Mqtt.UIHints;

/// <summary>
/// Provides human-readable labels for MQTT Quality of Service levels in the Elsa Studio dropdown.
/// </summary>
internal class MqttQosLevelDropdownOptionsProvider : DropDownOptionsProviderBase
{
    private static readonly ICollection<SelectListItem> Items =
    [
        new SelectListItem("0 - At Most Once",  "0"),
        new SelectListItem("1 - At Least Once", "1"),
        new SelectListItem("2 - Exactly Once",  "2"),
    ];

    protected override ValueTask<ICollection<SelectListItem>> GetItemsAsync(
        PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Items);
}
