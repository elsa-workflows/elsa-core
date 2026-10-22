using Elsa.Workflows.Models;

namespace Elsa.Workflows.Activities.Compensation;

internal sealed class CompensationScopeConstructionPropertiesModifier : IActivityDescriptorModifier
{
    public void Modify(ActivityDescriptor descriptor)
    {
        if (descriptor.ClrType != typeof(CompensationScope) ||
            descriptor.TypeName != CompensationScope.ActivityTypeName ||
            descriptor.Version != CompensationScope.CurrentDraftSchemaVersion)
            return;

        descriptor.ConstructionProperties[nameof(CompensationScope.DraftSchemaVersion)] =
            CompensationScope.CurrentDraftSchemaVersion;
        descriptor.ConstructionProperties[nameof(CompensationScope.Steps)] = new List<CompensationStep>();
        descriptor.ConstructionProperties[nameof(CompensationScope.OnForwardFault)] =
            CompensationFaultHandoff.Compensated;
    }
}
