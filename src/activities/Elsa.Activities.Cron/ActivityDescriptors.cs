using System;
using System.Collections.Generic;
using Elsa.Activities.Cron.Activities;
using Microsoft.Extensions.Localization;

namespace Elsa.Activities.Cron
{
    public class ActivityProvider : CombinedActivityProviderBase
    {
        public ActivityProvider(IStringLocalizer<ActivityProvider> localizer) : base(localizer)
        {
        }

        protected override IEnumerable<Type> Describe()
        {
            yield return typeof(CronTrigger);
        }
    }
}