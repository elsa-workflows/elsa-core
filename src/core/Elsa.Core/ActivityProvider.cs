using System;
using System.Collections.Generic;
using Elsa.Core.Activities;
using Microsoft.Extensions.Localization;

namespace Elsa.Core
{
    public class ActivityProvider : CombinedActivityProviderBase
    {
        public ActivityProvider(IStringLocalizer<ActivityProvider> localizer) : base(localizer)
        {
        }
        
        protected override IEnumerable<Type> Describe()
        {
            yield return typeof(UnknownActivity);
        }
    }
}