using System.Collections.Generic;
using System.Linq;
using Elsa.Activities;
using Elsa.Models;
using Elsa.Results;
using Microsoft.Extensions.Localization;

namespace Elsa.Handlers
{
    public class UnknownActivityDescriptor : ActivityDescriptorBase<UnknownActivity>
    {
        public UnknownActivityDescriptor(IStringLocalizer<UnknownActivityDescriptor> localizer)
        {
            T = localizer;
        }

        public IStringLocalizer T { get; }
        public override LocalizedString Category => T["System"];
        protected override IEnumerable<LocalizedString> GetEndpoints() => Enumerable.Empty<LocalizedString>();
        
    }
}