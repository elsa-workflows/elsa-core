using Elsa.Workflows.Runtime.Options;

namespace Elsa.Workflows.Runtime.UnitTests.Options;

public class BookmarkQueuePurgeOptionsTests
{
    [Fact(DisplayName = "Default TTL keeps queue items long enough for periodic retries")]
    public void DefaultTtl_KeepsQueueItemsLongEnoughForPeriodicRetries()
    {
        var options = new BookmarkQueuePurgeOptions();

        Assert.Equal(TimeSpan.FromHours(1), options.Ttl);
    }
}
