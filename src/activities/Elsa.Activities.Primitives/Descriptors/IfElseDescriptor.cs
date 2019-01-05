using System.Collections.Generic;
using Elsa.Activities.Primitives.Activities;
using Elsa.Handlers;
using Microsoft.Extensions.Localization;

namespace Elsa.Activities.Primitives.Descriptors
{
    public class IfElseDescriptor : ActivityDescriptorBase<IfElse>
    {
        public IfElseDescriptor(IStringLocalizer<IfElseDescriptor> localizer)
        {
            T = localizer;
        }

        public IStringLocalizer T { get; }
        public override LocalizedString Category => T["Control Flow"];
        public override LocalizedString DisplayText => T["If/Else Branch"];
        public override LocalizedString Description => T["Evaluate a boolean condition and continues execution based on the outcome."];
        protected override IEnumerable<LocalizedString> GetEndpoints() => Endpoints(T["True"], T["False"]);
    }
}