using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Stores.InMemory;

namespace Elsa.ExternalAuthentication.IntegrationTests.Operations;

public class PreviewSignInTests
{
    [Test]
    public async Task PreviewResultIsAdministratorBoundOneTimeAndDoesNotCreateSessionOrCredentials()
    {
        var clock = new TestClock(DateTimeOffset.UtcNow);
        var previews = new InMemoryPreviewResultStore(clock);
        var preview = new PreviewResult("handle", "admin-a", "tenant-a", "connection-a", "revision", "https://issuer.example", "su***ct", new Dictionary<string, IReadOnlyCollection<string>> { ["email"] = ["[REDACTED]"] }, "would_reject_unlinked_identity", [], [], clock.UtcNow.AddMinutes(1), null);
        await previews.SaveAsync(preview);

        var wrongAdministrator = await previews.TryTakeAsync("handle", "admin-b");
        var first = await previews.TryTakeAsync("handle", "admin-a");
        var second = await previews.TryTakeAsync("handle", "admin-a");

        await Assert.That(wrongAdministrator).IsOfType(typeof(TakeResult<PreviewResult>.NotFound));
        await Assert.That(first).IsOfType(typeof(TakeResult<PreviewResult>.Taken));
        await Assert.That(second).IsOfType(typeof(TakeResult<PreviewResult>.AlreadyConsumed));
        var value = ((TakeResult<PreviewResult>.Taken)first).Value;
        await Assert.That(value.ConnectionId).IsEqualTo("connection-a");
        await Assert.That(value.ProjectedClaims.SelectMany(x => x.Value)).DoesNotContain(x => x.Contains("token", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class TestClock(DateTimeOffset now) : Elsa.Common.ISystemClock { public DateTimeOffset UtcNow { get; set; } = now; }
}
