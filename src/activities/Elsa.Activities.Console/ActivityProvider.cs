using System;
using System.Collections.Generic;
using Elsa.Activities.Console.Activities;
using Microsoft.Extensions.Localization;

namespace Elsa.Activities.Console
{
    public class ActivityProvider : CombinedActivityProviderBase
    {
        public ActivityProvider(IStringLocalizer<ActivityProvider> localizer) : base(localizer)
        {
        }
        
        protected override IEnumerable<Type> Describe()
        {
            yield return typeof(ReadLine);
            yield return typeof(WriteLine);
        }
    }
}