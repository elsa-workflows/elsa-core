using System.Collections.Generic;
using System.Linq;
using Elsa.Services.Models;

namespace Elsa.Services.Workflows
{
    /// <summary>
    /// Default implementation of <see cref="IGetsStartActivities"/>.
    /// </summary>
    public class GetsStartActivitiesProvider : IGetsStartActivities
    {
        /// <summary>
        /// Gets a collection of the starting activities for the specified composite activity blueprint.
        /// </summary>
        /// <param name="compositeActivityBlueprint">A composite activity blueprint</param>
        /// <returns>A collection of the blueprint's starting activities</returns>
        public IEnumerable<IActivityBlueprint> GetStartActivities(ICompositeActivityBlueprint compositeActivityBlueprint)
        {
            var activityIdsThatAreNotStartingActivities = GetAllActivityIdsWhichHaveInboundConnections(compositeActivityBlueprint);

            var query = from activity in compositeActivityBlueprint.Activities
                        where !activityIdsThatAreNotStartingActivities.Contains(activity.Id)
                        select activity;

            return query;
        }

        /// <summary>
        /// This method gets activities that have inbound connections.
        /// </summary>
        /// <remarks>
        /// <para>
        /// "Start activities" are those with no inbound connections; IE no workflow connection-target will point to a start activity.
        /// What this method returns is essentially a blacklist of activity IDs which are not starting activities.
        /// </para>
        /// </remarks>
        /// <param name="compositeActivityBlueprint">A composite activity blueprint</param>
        /// <returns>A lookup of activity IDs which are not starting activities</returns>
        ILookup<string?,string?> GetAllActivityIdsWhichHaveInboundConnections(ICompositeActivityBlueprint compositeActivityBlueprint)
        {
            return compositeActivityBlueprint.Connections
                .Select(x => x.Target.Activity?.Id)
                .Distinct()
                .ToLookup(x => x);
        }
    }
}