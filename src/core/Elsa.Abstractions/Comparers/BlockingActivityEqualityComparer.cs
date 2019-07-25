using System.Collections.Generic;
using Elsa.Models;

namespace Elsa.Comparers
{
    public class BlockingActivityEqualityComparer : IEqualityComparer<BlockingActivity>
    {
        public bool Equals(BlockingActivity x, BlockingActivity y)
        {
            return x.ActivityId.Equals(y.ActivityId);
        }

        public int GetHashCode(BlockingActivity obj)
        {
            return obj.ActivityId.GetHashCode();
        }
    }
}