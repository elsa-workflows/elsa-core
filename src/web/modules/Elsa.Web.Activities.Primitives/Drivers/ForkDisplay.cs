using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Elsa.Activities.Primitives.Activities;
using Elsa.Web.Activities.Primitives.ViewModels;
using Elsa.Web.Drivers;
using Enumerable = System.Linq.Enumerable;

namespace Elsa.Web.Activities.Primitives.Drivers
{
    public class ForkDisplay : ActivityDisplayDriver<Fork, ForkViewModel>
    {
        protected override void EditActivity(Fork activity, ForkViewModel model)
        {
            model.Forks = string.Join(", ", activity.Forks ?? Enumerable.Empty<string>());
        }

        protected override void UpdateActivity(ForkViewModel model, Fork activity)
        {
            activity.Forks = !string.IsNullOrWhiteSpace(model.Forks)
                ? (IList<string>) model.Forks
                    .Split(new[] { "," }, StringSplitOptions.None)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList()
                : new string[0];
        }
    }
}