using System.Collections.Generic;
using System.Linq;
using Elsa.Activities.Http.Activities;
using Elsa.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;

namespace Elsa.Activities.Http
{
    public class ActivityDescriptors : ActivityDescriptorProviderBase
    {
        public ActivityDescriptors(IStringLocalizer<ActivityDescriptors> localizer)
        {
            T = localizer;
        }

        private IStringLocalizer<ActivityDescriptors> T { get; }
        private LocalizedString Category => T["HTTP"];

        protected override IEnumerable<ActivityDescriptor> Describe()
        {
            yield return ActivityDescriptor.ForTrigger<HttpRequestTrigger>(
                Category,
                T["HTTP Request Trigger"],
                T["Triggers when an incoming HTTP request is received."],
                T["Done"]);
            
            yield return ActivityDescriptor.For<HttpRequestAction>(
                Category,
                T["HTTP Request"],
                T["Execute a HTTP request."],
                true,
                true,
                a => a.SupportedStatusCodes.Select(x => T[x.ToString()]));
            
            yield return ActivityDescriptor.ForAction<HttpResponseAction>(
                Category,
                T["HTTP Response"],
                T["Write a HTTP response."],
                T["Done"]);
        }
    }
}