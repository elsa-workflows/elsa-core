using System.Reflection;
using Elsa.Sql.Contracts;
using Elsa.Workflows.UIHints.Dropdown;

namespace Elsa.Sql.UIHints;

/// <summary>
/// Provides registered clients for the Client input field.
/// </summary>
/// <param name="sqlClientNamesProvider"></param>
public class SqlClientsDropDownProvider(ISqlClientNamesProvider sqlClientNamesProvider) : DropDownOptionsProviderBase
{
    protected override async ValueTask<ICollection<SelectListItem>> GetItemsAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken)
    {
        var clients = await sqlClientNamesProvider.GetRegisteredSqlClientNamesAsync(cancellationToken);
        return clients.Select(x => new SelectListItem(x.Key, x.Key)).ToList();
    }
}