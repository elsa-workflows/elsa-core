using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Primitives.Activities;
using Elsa.Handlers;
using Elsa.Models;
using Elsa.Results;
using Microsoft.Extensions.Localization;

namespace Elsa.Activities.Primitives.Descriptors
{
    public class ForEachDescriptor : ActivityDescriptorBase<ForEach>
    {
        public ForEachDescriptor(IStringLocalizer<ForEachDescriptor> localizer)
        {
            T = localizer;
        }

        public IStringLocalizer T { get; }
        public override LocalizedString Category => T["Control Flow"];
        public override LocalizedString DisplayText => T["For Each"];
        public override LocalizedString Description => T["Iterate over a list of items."];
        protected override IEnumerable<LocalizedString> GetEndpoints() => Endpoints(T["Next"], T["Done"]);
    }
}