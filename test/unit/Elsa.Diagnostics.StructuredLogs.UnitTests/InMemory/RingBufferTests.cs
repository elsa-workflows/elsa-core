using Elsa.Diagnostics.StructuredLogs.Providers.InMemory;
using System.Threading.Tasks;

namespace Elsa.Diagnostics.StructuredLogs.UnitTests.InMemory;

public class RingBufferTests
{
    [Test]
    public async Task Add_WhenCapacityIsExceeded_DropsOldestItems()
    {
        var buffer = new RingBuffer<int>(3);

        buffer.Add(1);
        buffer.Add(2);
        buffer.Add(3);
        buffer.Add(4);

        await Assert.That(buffer.Snapshot()).IsEquivalentTo([2, 3, 4], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(buffer.DroppedCount).IsEqualTo(1);
    }

    [Test]
    public void Constructor_WhenCapacityIsZero_Throws()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new RingBuffer<int>(0));
    }
}
