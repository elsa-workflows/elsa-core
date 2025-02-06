using System.Reflection;

namespace Elsa.Workflows.UIHints.SqlEditor;

/// <inheritdoc />
public class SqlEditorUIHintHandler : IUIHintHandler
{
    /// <inheritdoc />
    public string UIHint => InputUIHints.SqlEditor;

    /// <inheritdoc />
    public ValueTask<IEnumerable<Type>> GetPropertyUIHandlersAsync(PropertyInfo propertyInfo, CancellationToken cancellationToken)
    {
        return new([typeof(SqlCodeOptionsProvider)]);
    }
}