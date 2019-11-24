using System.Collections.Generic;
using Elsa.Models;

namespace Elsa.Comparers
{
    public class BlockingActivityEqualityComparer : IEqualityComparer<BlockingActivity>
    {
        public bool Equals(BlockingActivity x, BlockingActivity y) => x.ActivityId.Equals(y.ActivityId);
        public int GetHashCode(BlockingActivity obj) => obj.ActivityId.GetHashCode();
    }
}