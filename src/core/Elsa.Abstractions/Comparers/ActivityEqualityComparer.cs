using System.Collections.Generic;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Comparers
{
    public class ActivityEqualityComparer : IEqualityComparer<IActivity>
    {
        public bool Equals(IActivity x, IActivity y) => x.Id.Equals(y.Id);
        public int GetHashCode(IActivity obj) => obj.Id.GetHashCode();
    }
}