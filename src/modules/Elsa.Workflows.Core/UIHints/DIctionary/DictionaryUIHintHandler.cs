using System.Reflection;

namespace Elsa.Workflows.UIHints.Dictionary;

/// <inheritdoc />
public class DictionaryUIHintHandler : IUIHintHandler
{
    /// <inheritdoc />
    public string UIHint => InputUIHints.Dictionary;

    /// <inheritdoc />
    public ValueTask<IEnumerable<Type>> GetPropertyUIHandlersAsync(PropertyInfo propertyInfo, CancellationToken cancellationToken)
    {
        return new ValueTask<IEnumerable<Type>>([typeof(StaticDictionaryOptionsProvider)]);
    }
}
