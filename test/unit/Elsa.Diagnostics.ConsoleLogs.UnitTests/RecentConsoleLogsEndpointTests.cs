using System.Text;
using System.Text.Json;
using Elsa.Diagnostics.ConsoleLogs.Endpoints.ConsoleLogs.Recent;
using System.Threading.Tasks;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests;

public class RecentConsoleLogsEndpointTests
{
    [Test]
    public async Task DeserializeJsonBodyAsync_WhenJsonIsMalformed_ThrowsJsonException()
    {
        await using var body = new MemoryStream(Encoding.UTF8.GetBytes("{"));

        await Assert.ThrowsExactlyAsync<JsonException>(async () => await Endpoint.DeserializeJsonBodyAsync(body, CancellationToken.None));
    }
}
