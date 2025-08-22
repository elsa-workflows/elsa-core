using System.Reflection;
using Elsa.Logging.Contracts;
using Elsa.Workflows.UIHints.CheckList;

namespace Elsa.Logging.UI;

/// <summary>
/// Provides checklist options for UI components to select log sinks. This class retrieves available log sinks
/// from the provided <see cref="ILogSinkCatalog"/> and maps them to checklist items for UI rendering purposes.
/// </summary>
public class LogSinkCheckListUIHintHandler(ILogSinkCatalog catalog) : CheckListOptionsProviderBase
{
    protected override async ValueTask<ICollection<CheckListItem>> GetItemsAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken)
    {
        var sinks = await catalog.ListAsync(cancellationToken);
        return sinks.Select(x => new CheckListItem(x.Name, x.Name)).ToList();
    }
}