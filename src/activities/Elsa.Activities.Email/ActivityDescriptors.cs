using System;
using System.Collections.Generic;
using Elsa.Activities.Email.Activities;
using Microsoft.Extensions.Localization;

namespace Elsa.Activities.Email
{
    public class ActivityProvider : CombinedActivityProviderBase
    {
        public ActivityProvider(IStringLocalizer<ActivityProvider> localizer) : base(localizer)
        {
        }

        protected override IEnumerable<Type> Describe() => new[] { typeof(SendEmail) };
    }
}