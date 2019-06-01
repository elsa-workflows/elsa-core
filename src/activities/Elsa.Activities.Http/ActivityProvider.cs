using System.Collections.Generic;
using Elsa.Activities.Http.Activities;
using Elsa.Models;

namespace Elsa.Activities.Http
{
    public class ActivityProvider : ActivityProviderBase
    {
        protected override IEnumerable<ActivityDescriptor> Describe()
        {
            yield return ActivityDescriptor.For<HttpRequestTrigger>();
            yield return ActivityDescriptor.For<HttpResponseAction>();
            yield return ActivityDescriptor.For<HttpRequestAction>();
        }
    }
}