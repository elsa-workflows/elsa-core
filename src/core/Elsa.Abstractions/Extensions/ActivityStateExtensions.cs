using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Services;

namespace Elsa.Extensions
{
    public static class ActivityStateExtensions
    {
        public static IActivity? FindActivity(this Variables state, string id)
        {
            var activities = state.SelectActivities().ToList();
            return activities.FirstOrDefault(x => x.Id == id);
        }
        
        public static IEnumerable<IActivity> SelectActivities(this Variables state)
        {
            var individualActivities = state.Values.Where(x => x.Value is IActivity).Select(x => x.Value).Cast<IActivity>().ToList();
            var activityCollections = state.Values.Where(x => x.Value is IEnumerable<IActivity>).Select(x => x.Value).Cast<IEnumerable<IActivity>>().SelectMany(x => x).ToList();
            var activities = individualActivities.Concat(activityCollections).ToList();

            return activities.Concat(activities.SelectMany(x => x.State.SelectActivities()));
        }
    }
}