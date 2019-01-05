using System.Collections.Generic;
using Elsa.Activities.Console.Activities;
using Elsa.Models;
using Microsoft.Extensions.Localization;

namespace Elsa.Activities.Console
{
    public class ActivityDescriptors : ActivityDescriptorProviderBase
    {
        public ActivityDescriptors(IStringLocalizer<ActivityDescriptors> localizer)
        {
            T = localizer;
        }

        private IStringLocalizer<ActivityDescriptors> T { get; }
        private LocalizedString Category => T["Console"];

        protected override IEnumerable<ActivityDescriptor> Describe()
        {
            yield return ActivityDescriptor.ForAction<ReadLine>(
                Category,
                T["Read Line"],
                T["Read a line from the console."],
                T["Done"]);
            
            yield return ActivityDescriptor.ForAction<WriteLine>(
                Category,
                T["Write Line"],
                T["Write a line to the console."],
                T["Done"]);
        }
    }
}