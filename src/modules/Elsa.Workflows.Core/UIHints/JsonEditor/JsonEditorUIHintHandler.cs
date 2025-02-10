using System.Reflection;

namespace Elsa.Workflows.UIHints.JsonEditor;

/// <inheritdoc />
public class JsonEditorUIHintHandler : IUIHintHandler
{
    /// <inheritdoc />
    public string UIHint => InputUIHints.JsonEditor;

    /// <inheritdoc />
    public ValueTask<IEnumerable<Type>> GetPropertyUIHandlersAsync(PropertyInfo propertyInfo, CancellationToken cancellationToken)
    {
        return new([typeof(JsonCodeOptionsProvider)]);
    }
}