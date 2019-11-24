using System.Collections.Generic;
using Elsa.Models;

namespace Elsa.Comparers
{
    public class ActivityInstanceEqualityComparer : IEqualityComparer<ActivityInstance>
    {
        public bool Equals(ActivityInstance x, ActivityInstance y) => x.Id.Equals(y.Id);
        public int GetHashCode(ActivityInstance obj) => obj.Id.GetHashCode();
    }
}