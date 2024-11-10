using System.Reflection;
using Elsa.Workflows.UIHints.Dropdown;
using Open.Linq.AsyncExtensions;

namespace Elsa.Kafka.UIHints;

public class TopicDefinitionsDropdownOptionsProvider(ITopicDefinitionEnumerator topicEnumerator) : DropDownOptionsProviderBase
{
    protected override async ValueTask<ICollection<SelectListItem>> GetItemsAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken)
    {
        var definitions = await topicEnumerator.EnumerateAsync(cancellationToken).ToList();
        return definitions.Select(x => new SelectListItem(x.Name, x.Id)).ToList();
    }
}