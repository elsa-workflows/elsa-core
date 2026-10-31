using System.Reflection;

namespace Elsa.Workflows.UIHints.RadioList;

/// <inheritdoc />
public class RadioListUIHintHandler : IUIHintHandler
{
    /// <inheritdoc />
    public string UIHint => InputUIHints.RadioList;

    /// <inheritdoc />
    public ValueTask<IEnumerable<Type>> GetPropertyUIHandlersAsync(PropertyInfo propertyInfo, CancellationToken cancellationToken)
    {
        return new([typeof(StaticRadioListOptionsProvider)]);
    }
}