using System.Collections.Generic;
using Elsa.Activities.Email.Activities;
using Elsa.Models;
using Microsoft.Extensions.Localization;

namespace Elsa.Activities.Email
{
    public class ActivityDescriptors : ActivityDescriptorProviderBase
    {
        public ActivityDescriptors(IStringLocalizer<ActivityDescriptors> localizer)
        {
            T = localizer;
        }

        private IStringLocalizer<ActivityDescriptors> T { get; }
        private LocalizedString Category => T["Email"];

        protected override IEnumerable<ActivityDescriptor> Describe()
        {
            yield return ActivityDescriptor.ForAction<SendEmail>(
                Category,
                T["Send Email"],
                T["Send an email message via SMTP."],
                T["Done"]);
        }
    }
}