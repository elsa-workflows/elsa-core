using Elsa.Activities.Console.Activities;
using Elsa.Activities.Console.Drivers;
using Elsa.Handlers;
using Microsoft.Extensions.Localization;

namespace Elsa.Activities.Console.Descriptors
{
    /// <summary>
    /// Writes a text string to the specified stream.
    /// </summary>
    public class WriteLineDescriptor : ActivityDescriptorBase<WriteLine>
    {
        public WriteLineDescriptor(IStringLocalizer<WriteLineDriver> localizer)
        {
            T = localizer;
        }
        
        private IStringLocalizer T { get; }
        
        protected override LocalizedString GetEndpoint() => T["Done"];
        
        public override LocalizedString Category => T["Console"];
        public override LocalizedString DisplayText => T["Write Line"];
        public override LocalizedString Description => T["Write a line to the console"];
    }
}
