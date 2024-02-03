using System.Reflection;
using Elsa.Workflows.UIHints.Dropdown;

namespace Elsa.Samples.AspNet.CustomUIHandler;

/// <summary>
/// A custom dropdown options provider to provide vehicle options for the Brand property of <see cref="VehicleActivity"/>.
/// </summary>
public class VehicleUIHandler : DropDownOptionsProviderBase
{
    private readonly Random _random = new();

    protected override ValueTask<ICollection<SelectListItem>> GetItemsAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken)
    {
        var items = new List<SelectListItem>
        {
            new("BMW", "1"),
            new("Tesla", "2"),
            new("Peugeot", "3"),
            new(_random.Next(100).ToString(), "4")
        };

        return new(items);
    }
}