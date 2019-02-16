using System.Collections.Generic;
using Elsa.Activities.Cron.Activities;
using Elsa.Models;
using Microsoft.Extensions.Localization;

namespace Elsa.Activities.Cron
{
    public class ActivityDescriptors : ActivityDescriptorProviderBase
    {
        public ActivityDescriptors(IStringLocalizer<ActivityDescriptors> localizer)
        {
            T = localizer;
        }

        private IStringLocalizer<ActivityDescriptors> T { get; }
        private LocalizedString Category => T["Triggers"];

        protected override IEnumerable<ActivityDescriptor> Describe()
        {
            yield return ActivityDescriptor.ForTrigger<CronTrigger>(
                Category,
                T["Cron Trigger"],
                T["Triggers at specified intervals using CRON expressions."],
                T["Done"]);
        }
    }
}