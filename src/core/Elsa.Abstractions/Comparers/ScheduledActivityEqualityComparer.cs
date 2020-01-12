using System.Collections.Generic;
using Elsa.Models;

namespace Elsa.Comparers
{
    public class ScheduledActivityEqualityComparer : IEqualityComparer<ScheduledActivity>
    {
        public bool Equals(ScheduledActivity x, ScheduledActivity y) => string.Equals(x?.ActivityId, y?.ActivityId);
        public int GetHashCode(ScheduledActivity obj) => obj.ActivityId.GetHashCode();
    }
}