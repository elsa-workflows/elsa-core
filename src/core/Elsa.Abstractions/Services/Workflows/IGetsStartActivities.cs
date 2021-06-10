using System.Collections.Generic;
using Elsa.Services.Models;

namespace Elsa.Services
{
    /// <summary>
    /// An object which gets the starting activities for a specified <see cref="ICompositeActivityBlueprint"/>.
    /// </summary>
    public interface IGetsStartActivities
    {
        /// <summary>
        /// Gets a collection of the starting activities for the specified composite activity blueprint.
        /// </summary>
        /// <param name="compositeActivityBlueprint">A composite activity blueprint</param>
        /// <returns>A collection of the blueprint's starting activities</returns>
        IEnumerable<IActivityBlueprint> GetStartActivities(ICompositeActivityBlueprint compositeActivityBlueprint);
    }
}