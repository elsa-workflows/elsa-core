using System.Reflection;
using Elsa.Ldap.Options;
using Elsa.Workflows.UIHints.Dropdown;
using Microsoft.Extensions.Options;

namespace Elsa.Ldap.UIHints;

internal class LdapConnectionDropdownOptionsProvider(IOptions<LdapOptions> ldapOptions) : DropDownOptionsProviderBase
{
    protected override ValueTask<ICollection<SelectListItem>> GetItemsAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken)
    {
        var dropdownOptions = ldapOptions.Value.Connections.Keys
            .Select(connectionName => new SelectListItem(connectionName, connectionName))
            .ToList();
        
        return ValueTask.FromResult<ICollection<SelectListItem>>(dropdownOptions);
    }
}
