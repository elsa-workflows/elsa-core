using System.Reflection;
using Elsa.CSharp.Activities;

namespace Elsa.Workflows.UIHints.JsonEditor;

/// <inheritdoc />
public class JsonEditorUIHintHandler : IUIHintHandler
{
    /// <inheritdoc />
    public string UIHint => InputUIHints.JsonEditor;

    /// <inheritdoc />
    public ValueTask<IEnumerable<Type>> GetPropertyUIHandlersAsync(PropertyInfo propertyInfo, CancellationToken cancellationToken)
    {
        return new(new[] { typeof(JsonCodeOptionsProvider) });
    }
}