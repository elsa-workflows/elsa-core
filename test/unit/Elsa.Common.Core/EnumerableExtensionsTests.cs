using Elsa.Extensions;
using Xunit;

namespace Elsa.Common.Core;

public class EnumerableExtensionsTests
{
    [Fact]
    public void ToBatches_ReturnsItemsInListOfPages()
    {
        var items = new List<int>
        {
            1,
            2,
            3,
            4,
            5
        };
        var batchSize = 2;
        var batches = items.ToBatches(batchSize).ToList();
        
        Assert.Equal(3, batches.Count);
        Assert.Equal(2, batches.ElementAt(0).Count());
        Assert.Equal(2, batches.ElementAt(1).Count());
        Assert.Equal(1, batches.ElementAt(2).Count());
        Assert.Equal(3, batches.ElementAt(1).ElementAt(0));
        Assert.Equal(4, batches.ElementAt(1).ElementAt(1));
    }
}