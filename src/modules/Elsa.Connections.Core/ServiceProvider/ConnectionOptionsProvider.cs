using System.Reflection;
using Elsa.Connections.Attributes;
using Elsa.Connections.Persistence.Contracts;
using Elsa.Workflows.UIHints.Dropdown;

namespace Elsa.Connections.ServiceProvider;

public class ConnectionOptionsProvider : DropDownOptionsProviderBase
{
    private readonly IConnectionStore _store;

    public ConnectionOptionsProvider(IConnectionStore store)
    {
        _store = store;
    }
    public new async ValueTask<IDictionary<string, object>> GetUIPropertiesAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken = default)
    {
        var options = await base.GetUIPropertiesAsync(propertyInfo, context, cancellationToken);
        options.Add("Refresh", true);
        return options;
    }
    protected override async ValueTask<ICollection<SelectListItem>> GetItemsAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken)
    {
        var connection = new List<SelectListItem>();

        var connectionType = propertyInfo.GetCustomAttribute<ConnectionTypeAttribute>()?.Type;

        if (connectionType == null)
            return connection;

        var connections = await _store.FindManyAsync(new Persistence.Filters.ConnectionDefinitionFilter() { Type = connectionType.ToString() });

        foreach(var conn in connections)
            connection.Add(new SelectListItem(conn.Name, conn.Name));
        return connection;
    }

    protected override IDictionary<string, object> GetUIPropertyAdditionalOptions()
    {
        return new Dictionary<string, object>
        {
            ["Refresh"] = true
        };
    }
}
