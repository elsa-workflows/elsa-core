using System.Text;
using System.Text.Json;
using Elsa.Diagnostics.ConsoleLogs.Endpoints.ConsoleLogs.Recent;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests;

public class RecentConsoleLogsEndpointTests
{
    [Fact]
    public async Task DeserializeJsonBodyAsync_WhenJsonIsMalformed_ThrowsJsonException()
    {
        await using var body = new MemoryStream(Encoding.UTF8.GetBytes("{"));

        await Assert.ThrowsAsync<JsonException>(async () => await Endpoint.DeserializeJsonBodyAsync(body, CancellationToken.None));
    }
}
