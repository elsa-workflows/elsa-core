using System.Collections.Generic;
using System.Linq;
using Elsa.Services.Models;

namespace Elsa.Services
{
    /// <summary>
    /// Default implementation of <see cref="IGetsStartActivitiesForCompositeActivityBlueprint"/>.
    /// </summary>
    public class StartActivitiesForCompositeActivityBlueprintProvider : IGetsStartActivitiesForCompositeActivityBlueprint
    {
        /// <summary>
        /// Gets a collection of the starting activities for the specified composite activity blueprint.
        /// </summary>
        /// <param name="compositeActivityBlueprint">A composite activity blueprint</param>
        /// <returns>A collection of the blueprint's starting activities</returns>
        public IEnumerable<IActivityBlueprint> GetStartActivities(ICompositeActivityBlueprint compositeActivityBlueprint)
        {
            var targetActivityIds = compositeActivityBlueprint.Connections
                .Select(x => x.Target.Activity?.Id)
                .Distinct()
                .ToLookup(x => x);

            var query = from activity in compositeActivityBlueprint.Activities
                        where !targetActivityIds.Contains(activity.Id)
                        select activity;

            return query;
        }
    }
}