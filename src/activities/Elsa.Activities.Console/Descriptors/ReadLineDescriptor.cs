using Elsa.Activities.Console.Activities;
using Elsa.Activities.Console.Drivers;
using Elsa.Handlers;
using Microsoft.Extensions.Localization;

namespace Elsa.Activities.Console.Descriptors
{
    public class ReadLineDescriptor : ActivityDescriptorBase<ReadLine>
    {
        public ReadLineDescriptor(IStringLocalizer<ReadLineDriver> localizer)
        {
            T = localizer;
        }

        private IStringLocalizer T { get; }

        protected override LocalizedString GetEndpoint() => T["Done"];

        public override LocalizedString Category => T["Console"];
        public override LocalizedString DisplayText => T["Read Line"];
        public override LocalizedString Description => T["Read a line from the console"];
    }
}