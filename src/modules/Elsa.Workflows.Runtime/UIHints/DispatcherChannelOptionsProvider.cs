using System.Reflection;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.UIHints.Dropdown;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.UIHints;

/// <summary>
/// Provides options for activities that dispatch workflows to channels.
/// </summary>
public class DispatcherChannelOptionsProvider : DropDownOptionsProviderBase
{
    private readonly WorkflowDispatcherOptions _options;

    /// <inheritdoc />
    public DispatcherChannelOptionsProvider(IOptions<WorkflowDispatcherOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    protected override ValueTask<ICollection<SelectListItem>> GetItemsAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken)
    {
        var channelNames = _options.Channels.Select(x => x.Name).ToList();
        var selectListItems = new List<SelectListItem> { new("Default", "") };

        selectListItems.AddRange(channelNames.Select(x => new SelectListItem(x, x)));

        return new(selectListItems);
    }
}