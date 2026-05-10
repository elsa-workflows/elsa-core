using Elsa.Diagnostics.StructuredLogs.Providers.InMemory;

namespace Elsa.Diagnostics.StructuredLogs.UnitTests.InMemory;

public class RingBufferTests
{
    [Fact]
    public void Add_WhenCapacityIsExceeded_DropsOldestItems()
    {
        var buffer = new RingBuffer<int>(3);

        buffer.Add(1);
        buffer.Add(2);
        buffer.Add(3);
        buffer.Add(4);

        Assert.Equal([2, 3, 4], buffer.Snapshot());
        Assert.Equal(1, buffer.DroppedCount);
    }

    [Fact]
    public void Constructor_WhenCapacityIsZero_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RingBuffer<int>(0));
    }
}
