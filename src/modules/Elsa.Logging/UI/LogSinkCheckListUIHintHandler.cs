using System.Reflection;
using Elsa.Logging.Contracts;
using Elsa.Workflows.UIHints.CheckList;

namespace Elsa.Logging.UI;

public class LogSinkCheckListUIHintHandler(ILogSinkCatalog catalog) : CheckListOptionsProviderBase
{
    protected override async ValueTask<ICollection<CheckListItem>> GetItemsAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken)
    {
        var sinks = await catalog.ListAsync(cancellationToken);
        return sinks.Select(x => new CheckListItem(x.Name, x.Name)).ToList();
    }
}