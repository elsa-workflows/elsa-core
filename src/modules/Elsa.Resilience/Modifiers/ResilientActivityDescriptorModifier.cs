using Elsa.Workflows;
using Elsa.Workflows.Models;

namespace Elsa.Resilience.Modifiers;

/// <summary>
/// Modifies the <see cref="ActivityDescriptor"/> of the <see cref="Cron"/> activity.
/// </summary>
public class ResilientActivityDescriptorModifier : IActivityDescriptorModifier
{
    /// <inheritdoc />
    public void Modify(ActivityDescriptor descriptor)
    {
        if (!descriptor.CustomProperties.TryGetValue("Type", out var typeObj))
            return;
        
        if(typeObj is not Type type)
            return;
        
        // Check if this type implements IResilientActivity.
        if (!typeof(IResilientActivity).IsAssignableFrom(type))
            return;

        var categoryAttr = descriptor.Attributes.FirstOrDefault(x => x is ResilienceCategoryAttribute) as ResilienceCategoryAttribute;
        var category = categoryAttr?.Category ?? "Default";
        descriptor.CustomProperties["Resilient"] = true;
        descriptor.CustomProperties["ResilienceCategory"] = category;
    }
}