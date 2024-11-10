using System.Reflection;
using Elsa.Workflows.UIHints.Dropdown;
using Open.Linq.AsyncExtensions;

namespace Elsa.Kafka.UIHints;

public class ConsumerDefinitionsDropdownOptionsProvider(IConsumerDefinitionEnumerator consumerDefinitionEnumerator) : DropDownOptionsProviderBase
{
    protected override async ValueTask<ICollection<SelectListItem>> GetItemsAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken)
    {
        var definitions = await consumerDefinitionEnumerator.GetConsumerDefinitionsAsync(cancellationToken).ToList();
        return definitions.Select(x => new SelectListItem(x.Name, x.Id)).ToList();
    }
}