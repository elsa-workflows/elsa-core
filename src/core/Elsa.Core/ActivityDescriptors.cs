using System.Collections.Generic;
using Elsa.Activities;
using Elsa.Models;
using Microsoft.Extensions.Localization;

namespace Elsa
{
    public class ActivityDescriptors : ActivityDescriptorProviderBase
    {
        public ActivityDescriptors(IStringLocalizer<ActivityDescriptors> localizer)
        {
            T = localizer;
        }

        public IStringLocalizer T { get; }

        protected override IEnumerable<ActivityDescriptor> Describe()
        {
            yield return new ActivityDescriptor
            {
                ActivityType = typeof(UnknownActivity),
                Name = nameof(UnknownActivity),
                Category = T["System"],
                DisplayText = T["Unknown Activity"],
                Description = T["Displayed when an activity descriptor referenced form a workflow could not be found."],
                IsBrowsable = false
            };
        }
    }
}