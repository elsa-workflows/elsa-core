using Elsa.Connections.Attributes;
using Elsa.Workflows;

namespace Elsa.Connections.Filters;

public class PropertyAttributeFilter : ActivityStateFilterBase
{
    protected override ActivityStateFilterResult OnExecute(ActivityStateFilterContext context)
    {
        var inputDescriptor = context.InputDescriptor;

        if (Attribute.IsDefined(inputDescriptor.PropertyInfo, typeof(NoLogAttribute)))
        {
            var contextValue = context.Value.GetProperty("connectionName").GetString();

            if (contextValue == null)
                return ActivityStateFilterResult.Pass();

            var maskedValue = $"**** see connection information for {contextValue} ****";
            return Filtered(maskedValue);
        }

        return ActivityStateFilterResult.Pass();
    }
}
