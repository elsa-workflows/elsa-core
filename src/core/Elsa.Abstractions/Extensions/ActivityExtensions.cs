using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Services;

namespace Elsa.Extensions
{
    public static class ActivityExtensions
    {
        public static IActivity? FindActivity(this IActivity activity, string id) => activity.State.FindActivity(id);
        public static IEnumerable<IActivity> SelectActivities(this IActivity activity) => activity.State.SelectActivities();

        public static ActivityInstance ToActivityInstance(this IActivity activity) =>
            new ActivityInstance
            {
                Id = activity.Id,
                Type = activity.Type,
                Output = activity.Output,
                State = activity.State
            };
    }
}