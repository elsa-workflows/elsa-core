using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elsa.CommandExecuter.Contracts;
using Elsa.Workflows.UIHints.Dropdown;

namespace Elsa.CommandExecuter.Providers;
public class WorkflowCommandProvider : DropDownOptionsProviderBase
{

    protected override ValueTask<ICollection<SelectListItem>> GetItemsAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken)
    {
        var commandTypes = AppDomain.CurrentDomain.GetAssemblies()
            .Where(x => !x.IsDynamic)
           .SelectMany(x => x.GetExportedTypes())
            .Where(x => x.IsClass && !x.IsAbstract && !x.IsGenericType && typeof(WorkFlowCommand).IsAssignableFrom(x))
            .Select(x => new SelectListItem(Regex.Replace(x.Name, "([A-Z]{1,2}|[0-9]+)", " $1").TrimStart(), x.FullName))
            .OrderBy(e => e.Text).ToList();
        return new ValueTask<ICollection<SelectListItem>>(commandTypes);
    }
}