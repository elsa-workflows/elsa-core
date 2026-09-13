using System.Net;
using System.Net.Http.Json;
using CShells.Lifecycle;
using NSubstitute;

namespace Elsa.Shells.Api.Tests.Endpoints.Reload;

public class ReloadTests : ShellsApiTestBase
{
    private const string ShellId = "test-shell";

    [Test]
    public async Task Post_WhenSuccessful_Returns200WithCompletedStatus()
    {
        using var response = await HttpClient.PostAsync($"/shells/{ShellId}/reload", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await Assert.That(await response.Content.ReadFromJsonAsync<ShellReloadResult>(JsonOptions)).IsNotNull();
        await Assert.That(body.Status).IsEqualTo("Completed");
        await Assert.That(body.RequestedShellId).IsEqualTo(ShellId);
    }

    [Test]
    public async Task Post_WhenShellNotFound_Returns404WithNotFoundStatus()
    {
        ShellRegistry
            .ReloadAsync(ShellId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ReloadResult>(new ShellBlueprintNotFoundException(ShellId)));

        using var response = await HttpClient.PostAsync($"/shells/{ShellId}/reload", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        var body = await Assert.That(await response.Content.ReadFromJsonAsync<ShellReloadResult>(JsonOptions)).IsNotNull();
        await Assert.That(body.Status).IsEqualTo("NotFound");
        await Assert.That(body.RequestedShellId).IsEqualTo(ShellId);
        var message = await Assert.That(body.Message).IsNotNull();
        await Assert.That(message).Contains(ShellId, StringComparison.CurrentCulture);
    }

    [Test]
    public async Task Post_WhenReloadFails_Returns503WithFailedStatus()
    {
        ShellRegistry
            .ReloadAsync(ShellId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ReloadResult(ShellId, null, null, new InvalidOperationException("Shell reload failed"))));

        using var response = await HttpClient.PostAsync($"/shells/{ShellId}/reload", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.ServiceUnavailable);
        var body = await Assert.That(await response.Content.ReadFromJsonAsync<ShellReloadResult>(JsonOptions)).IsNotNull();
        await Assert.That(body.Status).IsEqualTo("Failed");
        await Assert.That(body.RequestedShellId).IsEqualTo(ShellId);
        await Assert.That(body.Message).IsEqualTo("Shell reload failed");
    }

    [Test]
    public async Task Post_WhenBlueprintUnavailable_Returns503WithFailedStatus()
    {
        ShellRegistry
            .ReloadAsync(ShellId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ReloadResult>(new ShellBlueprintUnavailableException(ShellId, new InvalidOperationException())));

        using var response = await HttpClient.PostAsync($"/shells/{ShellId}/reload", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.ServiceUnavailable);
        var body = await Assert.That(await response.Content.ReadFromJsonAsync<ShellReloadResult>(JsonOptions)).IsNotNull();
        await Assert.That(body.Status).IsEqualTo("Failed");
        await Assert.That(body.RequestedShellId).IsEqualTo(ShellId);
        var message = await Assert.That(body.Message).IsNotNull();
        await Assert.That(message).Contains(ShellId, StringComparison.CurrentCulture);
    }
}
