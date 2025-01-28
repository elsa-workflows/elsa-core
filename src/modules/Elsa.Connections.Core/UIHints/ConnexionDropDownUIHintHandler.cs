using System.Reflection;
using Elsa.Workflows;
using Elsa.Workflows.UIHints;
using Elsa.Workflows.UIHints.Dropdown;

namespace Elsa.Connections.UIHints;

public class ConnexionDropDownUIHintHandler : IUIHintHandler
{
    /// <inheritdoc />
    public string UIHint => $"connexion-{InputUIHints.DropDown}";

    /// <inheritdoc />
    public ValueTask<IEnumerable<Type>> GetPropertyUIHandlersAsync(PropertyInfo propertyInfo, CancellationToken cancellationToken)
    {
        return new(new[] { typeof(StaticDropDownOptionsProvider) });
    }
}