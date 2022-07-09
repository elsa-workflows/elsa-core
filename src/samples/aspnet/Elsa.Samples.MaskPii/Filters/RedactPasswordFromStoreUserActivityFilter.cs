using Elsa.DataMasking.Core.Abstractions;
using Elsa.DataMasking.Core.Models;
using Elsa.Samples.MaskPii.Activities;
using Elsa.Samples.MaskPii.Models;

namespace Elsa.Samples.MaskPii.Filters;

public class RedactPasswordFromStoreUserActivityFilter : ActivityStateFilter
{
    protected override void Apply(ActivityStateFilterContext context)
    {
        // We're only interested in our StoreUser activity.
        if (context.ActivityBlueprint.Type != nameof(StoreUser)) return;

        // Redact the password field.
        if (context.ActivityData["User"] is User user) context.ActivityData["User"] = user with { Password = "****" };
    }
}