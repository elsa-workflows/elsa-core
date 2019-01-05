using Elsa.Activities.Primitives.Activities;
using Elsa.Handlers;
using Microsoft.Extensions.Localization;

namespace Elsa.Activities.Primitives.Descriptors
{
    public class SetVariableDescriptor : ActivityDescriptorBase<SetVariable>
    {
        public SetVariableDescriptor(IStringLocalizer<SetVariableDescriptor> localizer)
        {
            T = localizer;
        }

        public IStringLocalizer T { get; }
        public override LocalizedString Category => T["Primitives"];
        public override LocalizedString DisplayText => T["Set Variable"];
        public override LocalizedString Description => T["Set a custom variable on the workflow."];
        protected override LocalizedString GetEndpoint() => T["Done"];
    }
}