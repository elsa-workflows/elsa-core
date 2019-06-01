using System.Collections.Generic;
using System.Linq;
using Elsa.Activities.Primitives.Activities;
using Elsa.Models;
using Microsoft.Extensions.Localization;

namespace Elsa.Activities.Primitives
{
    public class ActivityDesignerProvider : ActivityDesignerProviderBase
    {
        public ActivityDesignerProvider(IStringLocalizer<ActivityDesignerProvider> localizer)
        {
            T = localizer;
        }

        private IStringLocalizer T { get; }

        protected override IEnumerable<ActivityDesignerDescriptor> Describe()
        {
            yield return ActivityDesignerDescriptor.For<ForEach>(T);
            yield return ActivityDesignerDescriptor.For<Fork>(T,a => a.Forks.Select(x => T[x]));
            yield return ActivityDesignerDescriptor.For<Join>(T);
            yield return ActivityDesignerDescriptor.For<IfElse>(T);
            yield return ActivityDesignerDescriptor.For<SetVariable>(T);
            yield return ActivityDesignerDescriptor.For<Switch>(T, a => a.Cases.Select(x => T[x]));
        }
    }
}