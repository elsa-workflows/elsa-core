using System;
using Elsa.Web.Models;

namespace Elsa.Web.Extensions
{
    public static class ActivityExtensions
    {
        public static ActivityDesignerMetadata GetDesignerMetadata(this IActivity activity)
        {
            return activity.Metadata.CustomFields.GetValue("Designer", StringComparison.OrdinalIgnoreCase)?.ToObject<ActivityDesignerMetadata>() ?? new ActivityDesignerMetadata();
        }
    }
}