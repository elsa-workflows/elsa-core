using System.Collections.Generic;
using Elsa.Services.Models;

namespace Elsa.Comparers
{
    public class ActivityEqualityComparer : IEqualityComparer<IActivity>
    {
        public bool Equals(IActivity x, IActivity y)
        {
            return x.Id.Equals(y.Id);
        }

        public int GetHashCode(IActivity obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}