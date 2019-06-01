using System.Collections.Generic;
using Elsa.Activities;
using Elsa.Models;
using Microsoft.Extensions.Localization;

namespace Elsa
{
    public class ActivityProvider : ActivityProviderBase
    {
        protected override IEnumerable<ActivityDescriptor> Describe()
        {
            yield return ActivityDescriptor.For<UnknownActivity>();
        }
    }
}