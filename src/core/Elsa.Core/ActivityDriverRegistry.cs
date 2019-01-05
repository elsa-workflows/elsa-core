using System.Collections.Generic;
using System.Linq;

namespace Elsa
{
    public class ActivityDriverRegistry : IActivityDriverRegistry
    {
        private readonly IDictionary<string, IActivityDriver> dictionary;

        public ActivityDriverRegistry(IEnumerable<IActivityDriver> drivers)
        {
            dictionary = drivers.ToDictionary(x => x.ActivityType);
        }
        
        public IActivityDriver GetDriver(string activityTypeName)
        {
            return dictionary.ContainsKey(activityTypeName) ? dictionary[activityTypeName] : null;
        }
    }
}