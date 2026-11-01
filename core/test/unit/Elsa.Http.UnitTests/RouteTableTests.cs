using Elsa.Http.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Http.UnitTests;

public class RouteTableTests
{
    [Fact]
    public async Task Add_FromRouteTablesSharingOneCacheInParallel_KeepsEveryRoute()
    {
        const int count = 32;

        for (var attempt = 0; attempt < 20; attempt++)
        {
            using var cache = new MemoryCache(new MemoryCacheOptions());
            using var barrier = new Barrier(count);
            var tasks = Enumerable.Range(0, count).Select(i => Task.Factory.StartNew(() =>
            {
                var routeTable = new RouteTable(cache, NullLogger<RouteTable>.Instance);

                // Parks every thread before the first Add, so they all reach the cache entry creation together.
                barrier.SignalAndWait();
                routeTable.Add($"route-{i}");
            }, TaskCreationOptions.LongRunning)).ToArray();

            await Task.WhenAll(tasks);

            var routes = new RouteTable(cache, NullLogger<RouteTable>.Instance).Select(x => x.Route).ToList();
            Assert.Equal(count, routes.Count);
        }
    }
}
