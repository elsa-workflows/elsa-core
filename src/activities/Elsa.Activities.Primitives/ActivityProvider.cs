using System.Collections.Generic;
using System.Linq;
using Elsa.Activities.Primitives.Activities;
using Elsa.Models;
using Microsoft.Extensions.Localization;

namespace Elsa.Activities.Primitives
{
    public class ActivityProvider : ActivityProviderBase
    {
        protected override IEnumerable<ActivityDescriptor> Describe()
        {
            yield return ActivityDescriptor.For<ForEach>();
            yield return ActivityDescriptor.For<Fork>();
            yield return ActivityDescriptor.For<Join>();
            yield return ActivityDescriptor.For<IfElse>();
            yield return ActivityDescriptor.For<SetVariable>();
            yield return ActivityDescriptor.For<Switch>();
        }
    }
}