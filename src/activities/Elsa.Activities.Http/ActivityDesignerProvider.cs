using System.Collections.Generic;
using System.Linq;
using Elsa.Activities.Http.Activities;
using Elsa.Models;
using Microsoft.Extensions.Localization;

namespace Elsa.Activities.Http
{
    public class ActivityDesignerProvider : ActivityDesignerProviderBase
    {
        public ActivityDesignerProvider(IStringLocalizer<Activity> localizer)
        {
            T = localizer;
        }

        public IStringLocalizer T { get; }

        protected override IEnumerable<ActivityDesignerDescriptor> Describe()
        {
            yield return ActivityDesignerDescriptor.For<HttpRequestTrigger>(T);
            yield return ActivityDesignerDescriptor.For<HttpRequestAction>(T, a => a.SupportedStatusCodes.Select(x => T[x.ToString()]).ToArray());
            yield return ActivityDesignerDescriptor.For<HttpResponseAction>(T);
        }
    }
}