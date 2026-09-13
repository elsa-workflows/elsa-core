using System.Net;
using System.Net.Http.Json;
using CShells.Lifecycle;
using NSubstitute;

namespace Elsa.Shells.Api.Tests.Endpoints.ReloadAll;

public class ReloadAllTests : ShellsApiTestBase
{
    [Test]
    [DisplayName("Reload all: successful POST returns 200 with completed status")]
    public async Task Post_WhenSuccessful_Returns200WithCompletedStatus()
    {
        using var response = await HttpClient.PostAsync("/shells/reload", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await Assert.That(await response.Content.ReadFromJsonAsync<ShellReloadResult>(JsonOptions)).IsNotNull();
        await Assert.That(body.Status).IsEqualTo("Completed");
    }

    [Test]
    public async Task Post_WhenAnyShellFails_Returns503WithFailedStatus()
    {
        // CShells 0.0.15 surfaces per-shell failures via ReloadResult.Error rather than throwing.
        ShellRegistry
            .ReloadActiveAsync(Arg.Any<ReloadOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ReloadResult>>(new[]
            {
                new ReloadResult("shell-a", null, null, new InvalidOperationException("Shell reload failed"))
            }));

        using var response = await HttpClient.PostAsync("/shells/reload", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.ServiceUnavailable);
        var body = await Assert.That(await response.Content.ReadFromJsonAsync<ShellReloadResult>(JsonOptions)).IsNotNull();
        await Assert.That(body.Status).IsEqualTo("Failed");
        var message = await Assert.That(body.Message).IsNotNull();
        await Assert.That(message).Contains("Shell reload failed", StringComparison.CurrentCulture);
    }
}
