using System.Reflection;
using Elsa.Connections.Attributes;
using Elsa.Connections.Persistence.Contracts;
using Elsa.Workflows.UIHints.Dropdown;

namespace Elsa.Connections.ServiceProvider;

public class ConnectionOptionsProvider(IConnectionStore store) : DropDownOptionsProviderBase
{
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

        var connections = await store.FindManyAsync(new() { Type = connectionType.ToString() }, cancellationToken);

        foreach(var conn in connections)
            connection.Add(new(conn.Name, conn.Name));
        
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
