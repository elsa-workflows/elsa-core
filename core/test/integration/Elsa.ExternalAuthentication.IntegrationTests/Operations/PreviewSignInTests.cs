using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Stores.InMemory;

namespace Elsa.ExternalAuthentication.IntegrationTests.Operations;

public class PreviewSignInTests
{
    [Fact]
    public async Task PreviewResultIsAdministratorBoundOneTimeAndDoesNotCreateSessionOrCredentials()
    {
        var clock = new TestClock(DateTimeOffset.UtcNow);
        var previews = new InMemoryPreviewResultStore(clock);
        var preview = new PreviewResult("handle", "admin-a", "tenant-a", "connection-a", "revision", "https://issuer.example", "su***ct", new Dictionary<string, IReadOnlyCollection<string>> { ["email"] = ["[REDACTED]"] }, "would_reject_unlinked_identity", [], [], clock.UtcNow.AddMinutes(1), null);
        await previews.SaveAsync(preview);

        var wrongAdministrator = await previews.TryTakeAsync("handle", "admin-b");
        var first = await previews.TryTakeAsync("handle", "admin-a");
        var second = await previews.TryTakeAsync("handle", "admin-a");

        Assert.IsType<TakeResult<PreviewResult>.NotFound>(wrongAdministrator);
        Assert.IsType<TakeResult<PreviewResult>.Taken>(first);
        Assert.IsType<TakeResult<PreviewResult>.AlreadyConsumed>(second);
        var value = ((TakeResult<PreviewResult>.Taken)first).Value;
        Assert.Equal("connection-a", value.ConnectionId);
        Assert.DoesNotContain(value.ProjectedClaims.SelectMany(x => x.Value), x => x.Contains("token", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class TestClock(DateTimeOffset now) : Elsa.Common.ISystemClock { public DateTimeOffset UtcNow { get; set; } = now; }
}
